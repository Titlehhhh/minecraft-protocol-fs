using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McProtoFacts.Protocol.Rag;
using McProtoFacts.Protocol.Repository;
using Microsoft.Extensions.Logging;

namespace McpServer.Services;

/// <summary>
/// Built-in protocol search that works with zero external services:
/// BM25 over all chunks is always available; when an OpenAI-compatible
/// embedding endpoint is configured (RAG_EMBEDDING_BASE_URL/_MODEL, e.g.
/// LM Studio) a semantic ranking is added and fused via Reciprocal Rank
/// Fusion. Embeddings are cached on disk keyed by chunk ContentHash, so a
/// full re-embed only happens when facts actually change.
/// </summary>
public sealed class HybridSearchService
{
    private const int EmbedBatchSize = 16;
    private const int FusionDepth = 200;
    private const int RrfK = 60;

    private readonly IProtocolRepository _repository;
    private readonly RagOptions _options;
    private readonly RagEmbeddingClient _embeddingClient;
    private readonly ILogger<HybridSearchService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IReadOnlyList<ProtocolRagChunk>? _chunks;
    private LexicalSearchIndex? _lexical;
    private float[][]? _vectors;
    private string? _semanticError;

    public HybridSearchService(
        IProtocolRepository repository,
        RagOptions options,
        RagEmbeddingClient embeddingClient,
        ILogger<HybridSearchService> logger)
    {
        _repository = repository;
        _options = options;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    public LocalSearchStatus GetStatus() => new(
        _chunks is not null,
        _chunks?.Count ?? 0,
        _vectors is not null ? "hybrid" : "lexical",
        _options.EmbeddingConfigured,
        _options.EmbeddingModel ?? "",
        _vectors?.Length ?? 0,
        _semanticError,
        GetCachePath());

    public async Task<ProtocolChunkSearchResponse> SearchAsync(string query, int limit, CancellationToken ct)
    {
        query = query.Trim();
        if (query.Length == 0) return new ProtocolChunkSearchResponse(query, [], "lexical");

        await EnsureInitializedAsync(ct);
        var chunks = _chunks!;
        var lexical = _lexical!;
        limit = Math.Clamp(limit, 1, 50);

        var fused = new Dictionary<int, double>();
        foreach (var (rank, hit) in lexical.Search(query, FusionDepth).Index())
            fused[hit.Doc] = fused.GetValueOrDefault(hit.Doc) + 1.0 / (RrfK + rank + 1);

        var mode = "lexical";
        if (_vectors is not null)
        {
            var semantic = await TrySemanticRankAsync(query, ct);
            if (semantic is not null)
            {
                mode = "hybrid";
                foreach (var (rank, doc) in semantic.Index())
                    fused[doc] = fused.GetValueOrDefault(doc) + 1.0 / (RrfK + rank + 1);
            }
        }

        var owners = fused
            .OrderByDescending(pair => pair.Value)
            .Take(FusionDepth)
            .Select(pair => (Chunk: chunks[pair.Key], Score: pair.Value))
            .GroupBy(hit => $"{hit.Chunk.OwnerKind}:{hit.Chunk.OwnerId}", StringComparer.Ordinal)
            .Select(group => new ProtocolChunkSearchOwner(
                group.Key,
                group.First().Chunk.OwnerKind,
                group.First().Chunk.OwnerId,
                group.Max(hit => hit.Score),
                group.OrderByDescending(hit => hit.Score)
                    .Take(4)
                    .Select(hit => ToHit(hit.Chunk, hit.Score))
                    .ToArray()))
            .OrderByDescending(owner => owner.Score)
            .Take(limit)
            .ToArray();

        return new ProtocolChunkSearchResponse(query, owners, mode);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_chunks is not null) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_chunks is not null) return;

            var chunker = new ProtocolRagChunker(_repository, options: new ProtocolRagChunkOptions());
            var chunks = chunker.BuildChunks("all", null).Chunks.ToArray();
            var lexical = new LexicalSearchIndex(chunks.Select(chunk => chunk.Text).ToArray());
            _logger.LogInformation("Search index built: {Count} chunks (BM25).", chunks.Length);

            _vectors = await TryLoadVectorsAsync(chunks, ct);
            _lexical = lexical;
            _chunks = chunks;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<float[][]?> TryLoadVectorsAsync(IReadOnlyList<ProtocolRagChunk> chunks, CancellationToken ct)
    {
        if (!_options.EmbeddingConfigured)
        {
            _semanticError = "Embeddings not configured (RAG_EMBEDDING_BASE_URL + RAG_EMBEDDING_MODEL); lexical-only mode.";
            return null;
        }

        try
        {
            var cachePath = GetCachePath();
            var cache = LoadCache(cachePath);

            var missing = chunks.Where(chunk => !cache.ContainsKey(chunk.ContentHash)).ToArray();
            if (missing.Length > 0)
            {
                _logger.LogInformation("Embedding {Missing} of {Total} chunks via {Model}...", missing.Length, chunks.Count, _options.EmbeddingModel);
                for (var offset = 0; offset < missing.Length; offset += EmbedBatchSize)
                {
                    var batch = missing.Skip(offset).Take(EmbedBatchSize).ToArray();
                    var vectors = await _embeddingClient.EmbedAsync(batch.Select(chunk => chunk.Text).ToArray(), ct);
                    for (var i = 0; i < batch.Length; i++)
                        cache[batch[i].ContentHash] = Normalize(vectors[i]);
                }
                SaveCache(cachePath, cache);
            }

            _semanticError = null;
            return chunks.Select(chunk => cache[chunk.ContentHash]).ToArray();
        }
        catch (Exception ex)
        {
            _semanticError = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogWarning("Semantic ranking unavailable, falling back to lexical-only: {Error}", _semanticError);
            return null;
        }
    }

    private async Task<IReadOnlyList<int>?> TrySemanticRankAsync(string query, CancellationToken ct)
    {
        try
        {
            var vector = Normalize((await _embeddingClient.EmbedAsync([query], ct))[0]);
            var vectors = _vectors!;
            var scored = new (int Doc, float Score)[vectors.Length];
            for (var doc = 0; doc < vectors.Length; doc++)
                scored[doc] = (doc, Dot(vector, vectors[doc]));

            return scored
                .OrderByDescending(hit => hit.Score)
                .Take(FusionDepth)
                .Select(hit => hit.Doc)
                .ToArray();
        }
        catch (Exception ex)
        {
            _semanticError = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogWarning("Query embedding failed, lexical-only for this request: {Error}", _semanticError);
            return null;
        }
    }

    private string GetCachePath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "mcproto-facts");
        var model = _options.EmbeddingModel ?? "none";
        foreach (var invalid in Path.GetInvalidFileNameChars())
            model = model.Replace(invalid, '_');
        return Path.Combine(root, $"embed-cache-{model}.json");
    }

    private static Dictionary<string, float[]> LoadCache(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, float[]>>(File.ReadAllText(path))
                       ?? new Dictionary<string, float[]>(StringComparer.Ordinal);
        }
        catch (Exception)
        {
            // повреждённый кеш просто пересчитывается
        }

        return new Dictionary<string, float[]>(StringComparer.Ordinal);
    }

    private static void SaveCache(string path, Dictionary<string, float[]> cache)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(cache));
    }

    private static float[] Normalize(float[] vector)
    {
        var norm = MathF.Sqrt(Dot(vector, vector));
        if (norm == 0) return vector;
        for (var i = 0; i < vector.Length; i++) vector[i] /= norm;
        return vector;
    }

    private static float Dot(float[] a, float[] b)
    {
        var sum = 0f;
        var length = Math.Min(a.Length, b.Length);
        for (var i = 0; i < length; i++) sum += a[i] * b[i];
        return sum;
    }

    private static ProtocolChunkSearchHit ToHit(ProtocolRagChunk chunk, double score) => new(
        chunk.Id,
        chunk.OwnerKind,
        chunk.OwnerId,
        chunk.ChunkKind,
        chunk.Path,
        chunk.VersionRange,
        chunk.Text,
        chunk.Fields.ToArray(),
        chunk.Kinds.ToArray(),
        chunk.Categories.ToArray(),
        chunk.SemanticHints.ToArray(),
        chunk.RawPath,
        chunk.TextCharCount,
        chunk.EstimatedTokenCount,
        chunk.ContentHash,
        score);
}

public sealed record LocalSearchStatus(
    bool Ready,
    int ChunkCount,
    string Mode,
    bool EmbeddingConfigured,
    string EmbeddingModel,
    int CachedVectors,
    string? SemanticError,
    string CachePath);
