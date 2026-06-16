using System;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PacketGenerator.Protocol.Rag;
using PacketGenerator.Protocol.Repository;

namespace McpServer.Endpoints;

public static class ChunkEndpoints
{
    public static void MapChunkApi(this WebApplication app)
    {
        app.MapGet("/api/chunks/status", (QdrantChunkStore store) => Results.Ok(store.GetStatus()));

        app.MapGet("/api/chunks", (
            string? kind,
            string? filter,
            int? maxChars,
            IProtocolRepository repo) =>
        {
            try
            {
                var chunker = CreateChunker(repo, maxChars);
                return Results.Ok(chunker.BuildChunks(NormalizeKind(kind), filter));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/chunks/{**id}", (
            string id,
            string? kind,
            int? maxChars,
            IProtocolRepository repo) =>
        {
            try
            {
                var chunker = CreateChunker(repo, maxChars);
                var chunks = NormalizeKind(kind) == "type"
                    ? chunker.BuildTypeChunks(id)
                    : chunker.BuildPacketChunks(id);
                return Results.Ok(new ProtocolRagChunkSet(chunks));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapPost("/api/chunks/index", async (
            string? kind,
            string? filter,
            int? maxChars,
            IProtocolRepository repo,
            QdrantChunkStore store,
            CancellationToken ct) =>
        {
            try
            {
                var chunker = CreateChunker(repo, maxChars);
                var chunks = chunker.BuildChunks(NormalizeKind(kind), filter).Chunks;
                var result = await store.ReindexAsync([.. chunks], ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapPost("/api/chunks/search", async (
            ProtocolChunkSearchRequest request,
            QdrantChunkStore store,
            CancellationToken ct) =>
        {
            try
            {
                var result = await store.SearchAsync(request.Query, request.Limit, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });
    }

    private static ProtocolRagChunker CreateChunker(IProtocolRepository repository, int? maxChars)
    {
        var options = maxChars is > 0
            ? new ProtocolRagChunkOptions(MaxTextChars: maxChars.Value)
            : new ProtocolRagChunkOptions();
        return new ProtocolRagChunker(repository, options: options);
    }

    private static string NormalizeKind(string? kind) =>
        kind is "packet" or "type" or "all" ? kind : "all";
}
