using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PacketGenerator.Protocol.Rag;

namespace McpServer.Services;

public sealed class QdrantChunkStore
{
    private const int EmbedBatchSize = 16;
    private const int UpsertBatchSize = 64;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly RagOptions _options;
    private readonly RagEmbeddingClient _embeddingClient;

    public QdrantChunkStore(HttpClient http, RagOptions options, RagEmbeddingClient embeddingClient)
    {
        _http = http;
        _options = options;
        _embeddingClient = embeddingClient;
    }

    public ProtocolChunkStatus GetStatus() => new(
        _options.Enabled,
        _options.EmbeddingConfigured,
        _options.QdrantConfigured,
        _options.Collection,
        _options.Missing);

    public async Task<ProtocolChunkIndexResponse> ReindexAsync(IReadOnlyList<ProtocolRagChunk> chunks, CancellationToken ct)
    {
        EnsureEnabled();
        if (chunks.Count == 0) return new ProtocolChunkIndexResponse(0, 0, 0);

        var vectors = new List<float[]>(chunks.Count);
        for (var offset = 0; offset < chunks.Count; offset += EmbedBatchSize)
        {
            var batch = chunks.Skip(offset).Take(EmbedBatchSize).Select(chunk => chunk.Text).ToArray();
            vectors.AddRange(await _embeddingClient.EmbedAsync(batch, ct));
        }

        if (vectors.Count != chunks.Count)
            throw new InvalidOperationException($"Embedding service returned {vectors.Count} vectors for {chunks.Count} chunks.");

        await DeleteCollectionAsync(ct);
        await CreateCollectionAsync(vectors[0].Length, ct);

        for (var offset = 0; offset < chunks.Count; offset += UpsertBatchSize)
        {
            var points = chunks
                .Skip(offset)
                .Take(UpsertBatchSize)
                .Select((chunk, index) => ToPoint(chunk, vectors[offset + index]))
                .ToArray();

            var response = await _http.PutAsJsonAsync(
                $"{_options.QdrantUrl}/collections/{Uri.EscapeDataString(_options.Collection)}/points?wait=true",
                new { points },
                JsonOptions,
                ct);
            await EnsureSuccessAsync(response, "Qdrant upsert", ct);
        }

        return new ProtocolChunkIndexResponse(chunks.Count, vectors.Count, vectors[0].Length);
    }

    public async Task<ProtocolChunkSearchResponse> SearchAsync(string query, int limit, CancellationToken ct)
    {
        EnsureEnabled();
        query = query.Trim();
        if (query.Length == 0) return new ProtocolChunkSearchResponse(query, []);

        limit = Math.Clamp(limit, 1, 50);
        var vector = (await _embeddingClient.EmbedAsync([query], ct))[0];
        var searchLimit = Math.Max(limit * 6, 24);

        using var response = await _http.PostAsJsonAsync(
            $"{_options.QdrantUrl}/collections/{Uri.EscapeDataString(_options.Collection)}/points/search",
            new { vector, limit = searchLimit, with_payload = true },
            JsonOptions,
            ct);
        await EnsureSuccessAsync(response, "Qdrant search", ct);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var hits = json.RootElement
            .GetProperty("result")
            .EnumerateArray()
            .Select(ReadHit)
            .Where(hit => hit is not null)
            .Select(hit => hit!)
            .ToArray();

        var owners = hits
            .GroupBy(hit => $"{hit.OwnerKind}:{hit.OwnerId}", StringComparer.Ordinal)
            .Select(group => new ProtocolChunkSearchOwner(
                group.Key,
                group.First().OwnerKind,
                group.First().OwnerId,
                group.Max(hit => hit.Score),
                group.OrderByDescending(hit => hit.Score).Take(4).ToArray()))
            .OrderByDescending(owner => owner.Score)
            .Take(limit)
            .ToArray();

        return new ProtocolChunkSearchResponse(query, owners);
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("RAG vector search is disabled. Configure RAG_EMBEDDING_BASE_URL, RAG_EMBEDDING_MODEL, and RAG_QDRANT_URL.");
    }

    private async Task DeleteCollectionAsync(CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"{_options.QdrantUrl}/collections/{Uri.EscapeDataString(_options.Collection)}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return;
        if (!response.IsSuccessStatusCode) await EnsureSuccessAsync(response, "Qdrant delete collection", ct);
    }

    private async Task CreateCollectionAsync(int vectorSize, CancellationToken ct)
    {
        using var response = await _http.PutAsJsonAsync(
            $"{_options.QdrantUrl}/collections/{Uri.EscapeDataString(_options.Collection)}",
            new { vectors = new { size = vectorSize, distance = "Cosine" } },
            JsonOptions,
            ct);
        await EnsureSuccessAsync(response, "Qdrant create collection", ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"{operation} failed: HTTP {(int)response.StatusCode} {body}");
    }

    private static object ToPoint(ProtocolRagChunk chunk, float[] vector) => new
    {
        id = StableGuid(chunk.Id),
        vector,
        payload = new
        {
            Id = chunk.Id,
            OwnerKind = chunk.OwnerKind,
            OwnerId = chunk.OwnerId,
            ChunkKind = chunk.ChunkKind,
            Path = chunk.Path,
            VersionRange = chunk.VersionRange,
            Text = chunk.Text,
            Fields = chunk.Fields,
            Kinds = chunk.Kinds,
            Categories = chunk.Categories,
            SemanticHints = chunk.SemanticHints,
            RawPath = chunk.RawPath,
            TextCharCount = chunk.TextCharCount,
            EstimatedTokenCount = chunk.EstimatedTokenCount,
            ContentHash = chunk.ContentHash
        }
    };

    private static ProtocolChunkSearchHit? ReadHit(JsonElement item)
    {
        if (!item.TryGetProperty("payload", out var payload)) return null;
        var score = item.TryGetProperty("score", out var scoreElement) ? scoreElement.GetDouble() : 0;

        return new ProtocolChunkSearchHit(
            ReadString(payload, "id"),
            ReadString(payload, "ownerKind"),
            ReadString(payload, "ownerId"),
            ReadString(payload, "chunkKind"),
            ReadString(payload, "path"),
            ReadString(payload, "versionRange"),
            ReadString(payload, "text"),
            ReadStringArray(payload, "fields"),
            ReadStringArray(payload, "kinds"),
            ReadStringArray(payload, "categories"),
            ReadStringArray(payload, "semanticHints"),
            ReadString(payload, "rawPath"),
            ReadInt(payload, "textCharCount"),
            ReadInt(payload, "estimatedTokenCount"),
            ReadString(payload, "contentHash"),
            score);
    }

    private static string StableGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes.Take(16).ToArray()).ToString("D");
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property.GetString() ?? "" : "";

    private static int ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value) ? value : 0;

    private static string[] ReadStringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }
}

public sealed record ProtocolChunkStatus(
    bool Enabled,
    bool EmbeddingConfigured,
    bool QdrantConfigured,
    string Collection,
    string[] Missing);

public sealed record ProtocolChunkIndexResponse(int Chunks, int Vectors, int VectorSize);

public sealed record ProtocolChunkSearchRequest(string Query, int Limit);

public sealed record ProtocolChunkSearchResponse(string Query, IReadOnlyList<ProtocolChunkSearchOwner> Owners);

public sealed record ProtocolChunkSearchOwner(
    string Owner,
    string OwnerKind,
    string OwnerId,
    double Score,
    IReadOnlyList<ProtocolChunkSearchHit> Chunks);

public sealed record ProtocolChunkSearchHit(
    string Id,
    string OwnerKind,
    string OwnerId,
    string ChunkKind,
    string Path,
    string VersionRange,
    string Text,
    string[] Fields,
    string[] Kinds,
    string[] Categories,
    string[] SemanticHints,
    string RawPath,
    int TextCharCount,
    int EstimatedTokenCount,
    string ContentHash,
    double Score);
