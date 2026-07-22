using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace McpServer.Endpoints;

public static class ModelCatalogEndpoints
{
    public static void MapModelCatalogApi(this WebApplication app)
    {
        app.MapGet("/api/models/openrouter", async (
            string? q,
            string? kind,
            string? sort,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            try
            {
                var outputModality = string.Equals(kind, "embedding", StringComparison.OrdinalIgnoreCase)
                    ? "embeddings"
                    : "text";

                var query = new Dictionary<string, string?>
                {
                    ["output_modalities"] = outputModality,
                    ["sort"] = string.IsNullOrWhiteSpace(sort) ? "most-popular" : sort
                };

                if (!string.IsNullOrWhiteSpace(q))
                    query["q"] = q.Trim();

                var uri = QueryHelpers.AddQueryString("https://openrouter.ai/api/v1/models", query);
                var client = httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var apiKey = configuration["OpenRouter:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.Authorization = new("Bearer", apiKey);

                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    return Results.BadRequest(new
                    {
                        error = $"OpenRouter model search failed: HTTP {(int)response.StatusCode} {body}"
                    });
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var models = json.RootElement
                    .GetProperty("data")
                    .EnumerateArray()
                    .Select(ReadModel)
                    .Where(model => !string.IsNullOrWhiteSpace(model.Id))
                    .Take(40)
                    .ToArray();

                return Results.Ok(new OpenRouterModelSearchResponse(models));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });
    }

    private static OpenRouterModelItem ReadModel(JsonElement item)
    {
        var architecture = item.TryGetProperty("architecture", out var arch) ? arch : default;
        var pricing = item.TryGetProperty("pricing", out var price) ? price : default;

        return new OpenRouterModelItem(
            ReadString(item, "id"),
            ReadString(item, "name"),
            ReadInt(item, "context_length"),
            ReadString(item, "description"),
            ReadString(pricing, "prompt"),
            ReadString(pricing, "completion"),
            ReadStringArray(architecture, "input_modalities"),
            ReadStringArray(architecture, "output_modalities"));
    }

    private static string ReadString(JsonElement element, string name) =>
        element.ValueKind != JsonValueKind.Undefined &&
        element.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";

    private static int? ReadInt(JsonElement element, string name) =>
        element.ValueKind != JsonValueKind.Undefined &&
        element.TryGetProperty(name, out var property) &&
        property.TryGetInt32(out var value)
            ? value
            : null;

    private static string[] ReadStringArray(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Undefined ||
            !element.TryGetProperty(name, out var property) ||
            property.ValueKind != JsonValueKind.Array)
            return [];

        return property
            .EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }
}

public sealed record OpenRouterModelSearchResponse(IReadOnlyList<OpenRouterModelItem> Models);

public sealed record OpenRouterModelItem(
    string Id,
    string Name,
    int? ContextLength,
    string Description,
    string PromptPrice,
    string CompletionPrice,
    string[] InputModalities,
    string[] OutputModalities);
