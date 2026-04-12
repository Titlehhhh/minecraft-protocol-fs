using System.Threading;
using System.Threading.Tasks;
using McpServer.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace McpServer.Endpoints;

public static class ArtifactEndpoints
{
    public static void MapArtifactApi(this WebApplication app)
    {
        app.MapGet("/artifacts/{id}", async (
            string id,
            IArtifactsRepository artifacts,
            HttpContext http,
            CancellationToken ct) =>
        {
            var info = await artifacts.GetInfoAsync(id, ct);
            if (info is null) return Results.NotFound();

            var stream = await artifacts.OpenReadAsync(id, ct);
            if (stream is null) return Results.NotFound();

            http.Response.Headers.ContentDisposition = $"attachment; filename=\"{info.FileName}\"";
            return Results.Stream(stream, info.ContentType);
        }).WithName("GetArtifacts");
    }
}
