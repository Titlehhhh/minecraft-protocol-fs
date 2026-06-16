using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Services;

public sealed class RagEmbeddingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly RagOptions _options;

    public RagEmbeddingClient(HttpClient http, RagOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct)
    {
        if (!_options.EmbeddingConfigured)
            throw new InvalidOperationException("RAG embedding is disabled. Configure RAG_EMBEDDING_BASE_URL and RAG_EMBEDDING_MODEL.");

        if (inputs.Count == 0) return Array.Empty<float[]>();

        var request = new EmbeddingRequest(_options.EmbeddingModel!, inputs);
        var response = await _http.PostAsJsonAsync($"{_options.EmbeddingBaseUrl}/embeddings", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Embedding request failed: HTTP {(int)response.StatusCode} {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var data = json.RootElement.GetProperty("data")
            .EnumerateArray()
            .OrderBy(item => item.TryGetProperty("index", out var index) ? index.GetInt32() : 0)
            .ToArray();

        var vectors = new List<float[]>(data.Length);
        foreach (var item in data)
        {
            var embedding = item.GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
            vectors.Add(embedding);
        }

        return vectors;
    }

    private sealed record EmbeddingRequest(string Model, IReadOnlyList<string> Input);
}
