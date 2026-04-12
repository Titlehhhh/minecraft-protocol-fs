using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace McpServer.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigApi(this WebApplication app)
    {
        app.MapGet("/api/config", (ModelConfigService cfg) =>
            Results.Ok(cfg.Config));

        app.MapPost("/api/config", async (HttpContext http, ModelConfigService cfg, CancellationToken ct) =>
        {
            var updated = await JsonSerializer.DeserializeAsync<ModelConfig>(
                http.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            if (updated is null)
                return Results.BadRequest("Invalid config JSON.");

            cfg.Update(updated);
            Console.WriteLine($"[McpServer] Config updated: easy={updated.Easy.Model} medium={updated.Medium.Model} heavy={updated.Heavy.Model} thresh={updated.EasyComplexityThreshold}/{updated.HeavyComplexityThreshold}");
            return Results.Ok(cfg.Config);
        });
    }
}
