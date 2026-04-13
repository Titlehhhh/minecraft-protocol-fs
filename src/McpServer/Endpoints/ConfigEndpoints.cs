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

        app.MapPost("/api/config", async (ModelConfig updated, ModelConfigService cfg, CancellationToken ct) =>
        {
            await cfg.UpdateAsync(updated, ct);
            return Results.Ok(cfg.Config);
        });
    }
}
