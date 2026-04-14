using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace McpServer.Endpoints;

public static class GenerateEndpoints
{
    public static void MapGenerateApi(this WebApplication app)
    {
        // ── Prompt preview (dry-run) ──────────────────────────────────────────
        app.MapPost("/api/prompt", async (PromptRequest req, CodeGenerator gen, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Id))
                return Results.BadRequest("'id' must not be empty.");

            try
            {
                var (system, user, _) = await gen.BuildPromptAsync(req.Id, ct);
                var systemTokens = TokenCounter.Count(system);
                var userTokens   = TokenCounter.Count(user);
                return Results.Ok(new
                {
                    system,
                    user,
                    systemTokens,
                    userTokens,
                    tokenCount = systemTokens + userTokens,
                });
            }
            catch (System.Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        // ── Single generation ─────────────────────────────────────────────────
        app.MapPost("/api/generate", async (
            GenerateRequest     req,
            GenerationService   svc,
            IPacketFileService  fileService,
            ModelConfigService  mcs,
            CancellationToken   ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Id))
                return Results.BadRequest("'id' must not be empty.");

            var result = await svc.GenerateAsync(req.Id, ct);

            if (!result.IsSuccess)
                return Results.Problem(
                    detail:     result.Error.Message,
                    statusCode: 500,
                    extensions: new Dictionary<string, object?> { ["kind"] = result.Error.Kind.ToString() });

            var savedTo = string.IsNullOrWhiteSpace(mcs.Config.OutputBaseDir)
                ? null
                : await fileService.TrySaveAsync(result.Data, req.Id, mcs.Config.OutputBaseDir, ct);

            return Results.Ok(RestGenerationResult.From(result.Data, savedTo));
        });

        // ── Batch (returns all at once, no SSE) ───────────────────────────────
        app.MapPost("/api/generate/batch", async (
            string[]           ids,
            GenerationService  svc,
            CancellationToken  ct) =>
        {
            if (ids is null || ids.Length == 0)
                return Results.BadRequest("Provide at least one packet id.");

            var results = new List<RestGenerationResult>();
            await foreach (var r in svc.GenerateBatchAsync(ids, ct))
                results.Add(r.IsSuccess
                    ? RestGenerationResult.From(r.Data)
                    : new RestGenerationResult { Name = r.Id, Error = r.Error!.Message });

            return Results.Ok(results);
        });

        // ── Batch by namespace (SSE) ──────────────────────────────────────────
        app.MapPost("/api/generate/by-namespace", IResult (
            GenerateByNamespaceRequest req,
            GenerationService svc, IProtocolRepository proto,
            IPacketFileService fileService, ModelConfigService mcs,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Ns))
                return Results.BadRequest("Missing 'ns' field.");

            var all = proto.GetPackets();
            if (!all.TryGetValue(req.Ns, out var packets))
                return Results.BadRequest($"Unknown namespace '{req.Ns}'. Valid: {string.Join(", ", all.Keys)}");

            var ids = packets.Keys.Select(name => $"{req.Ns}.{name}").ToArray();
            return TypedResults.ServerSentEvents(ToSseStream(ids, svc, fileService, mcs, ct));
        });

        // ── Batch by tier (SSE) ───────────────────────────────────────────────
        app.MapPost("/api/generate/by-tier", IResult (
            GenerateByTierRequest req,
            GenerationService svc, IProtocolRepository proto,
            IPacketFileService fileService, ModelConfigService mcs,
            CancellationToken ct) =>
        {
            var validTiers = new[] { "tiny", "easy", "medium", "heavy" };
            if (string.IsNullOrWhiteSpace(req.Tier) || !System.Array.Exists(validTiers, t => t == req.Tier))
                return Results.BadRequest($"Invalid tier. Valid: {string.Join(", ", validTiers)}");

            var ids = proto.GetPackets()
                .SelectMany(kv => kv.Value.Keys.Select(name => $"{kv.Key}.{name}"))
                .Where(id =>
                {
                    try
                    {
                        var def = proto.GetPacket(id);
                        return mcs.ClassifyTier(PacketComplexityScorer.Compute(def.History)).ToLabel() == req.Tier;
                    }
                    catch { return false; }
                })
                .ToArray();

            return TypedResults.ServerSentEvents(ToSseStream(ids, svc, fileService, mcs, ct));
        });

        // ── Batch by explicit ids (SSE) ───────────────────────────────────────
        app.MapPost("/api/generate/batch-ids", IResult (
            GenerateBatchIdsRequest req,
            GenerationService svc, IPacketFileService fileService, ModelConfigService mcs,
            CancellationToken ct) =>
        {
            if (req.Ids is null || req.Ids.Length == 0)
                return Results.BadRequest("Missing or empty 'ids' array.");

            return TypedResults.ServerSentEvents(ToSseStream(req.Ids, svc, fileService, mcs, ct));
        });
    }

    // ── Shared SSE stream — единственное место с батч-логикой ─────────────────
    // JsonNode — явный JSON, нет проблем с source-generated сериализацией object/анонимных типов.
    private static async IAsyncEnumerable<JsonNode> ToSseStream(
        string[]           ids,
        GenerationService  svc,
        IPacketFileService fileService,
        ModelConfigService mcs,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new JsonObject { ["type"] = "start", ["total"] = ids.Length };

        int ok = 0, err = 0;

        await foreach (var result in svc.GenerateBatchAsync(ids, ct))
        {
            string? savedTo = null;
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(mcs.Config.OutputBaseDir))
                savedTo = await fileService.TrySaveAsync(result.Data, result.Id, mcs.Config.OutputBaseDir, ct);

            if (result.IsSuccess) ok++; else err++;

            yield return new JsonObject
            {
                ["type"]      = "packet",
                ["id"]        = result.Id,
                ["success"]   = result.IsSuccess,
                ["model"]     = result.Data?.Model,
                ["elapsedMs"] = result.Data?.ElapsedMs,
                ["savedTo"]   = savedTo,
                ["error"]     = result.Error?.Message,
                ["errorKind"] = result.Error?.Kind.ToString(),
            };
        }

        yield return new JsonObject { ["type"] = "done", ["total"] = ids.Length, ["ok"] = ok, ["err"] = err };
    }
}
