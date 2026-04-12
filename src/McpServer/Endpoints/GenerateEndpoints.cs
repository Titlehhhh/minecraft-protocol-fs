using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Helpers;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ProtoCore;

namespace McpServer.Endpoints;

public static class GenerateEndpoints
{
    public static void MapGenerateApi(this WebApplication app)
    {
        // ── Prompt preview (dry-run) ──────────────────────────────────────────
        app.MapPost("/api/prompt", async (HttpContext http, CodeGenerator gen, CancellationToken ct) =>
        {
            using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
            if (!body.RootElement.TryGetProperty("id", out var idEl))
                return Results.BadRequest("Missing 'id' field.");

            var id = idEl.GetString();
            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest("'id' must not be empty.");

            try
            {
                var (system, user, _) = await gen.BuildPromptAsync(id, ct);
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
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        // ── Single generation ─────────────────────────────────────────────────
        app.MapPost("/api/generate", async (HttpContext http, CodeGenerator gen, ModelConfigService mcs, CancellationToken ct) =>
        {
            using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
            if (!body.RootElement.TryGetProperty("id", out var idEl))
                return Results.BadRequest("Missing 'id' field.");

            var id = idEl.GetString();
            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest("'id' must not be empty.");

            var outputBaseDir = body.RootElement.TryGetProperty("outputBaseDir", out var dirEl)
                ? dirEl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(outputBaseDir))
                outputBaseDir = mcs.Config.OutputBaseDir;

            GenerationData data;
            try
            {
                data = await gen.GenerateAsync(id, ct);
            }
            catch (Exception ex)
            {
                var detail = ex is System.ClientModel.ClientResultException cre
                    ? $"{ex.GetType().Name}: {ex.Message}\nResponse body: {cre.GetRawResponse()?.Content?.ToString()}"
                    : $"{ex.GetType().Name}: {ex.Message}";
                Console.Error.WriteLine($"[Generate] ERROR for '{id}': {detail}");
                return Results.Problem(detail, statusCode: 500);
            }

            string? savedTo = null;
            if (!string.IsNullOrWhiteSpace(outputBaseDir) && !string.IsNullOrWhiteSpace(data.Code))
            {
                try
                {
                    var dir  = Path.Combine(outputBaseDir, PacketFileHelper.ResolveSubdir(id));
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, data.Name + ".cs");
                    await File.WriteAllTextAsync(path, data.Code, ct);
                    savedTo = path;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Generate] Failed to save file: {ex.Message}");
                }
            }

            return Results.Ok(RestGenerationResult.From(data, savedTo));
        });

        // ── Batch (simple, returns all at once) ───────────────────────────────
        app.MapPost("/api/generate/batch", async (HttpContext http, CodeGenerator gen, CancellationToken ct) =>
        {
            string[]? ids;
            try
            {
                ids = await JsonSerializer.DeserializeAsync<string[]>(http.Request.Body, cancellationToken: ct);
            }
            catch
            {
                return Results.BadRequest("Body must be a JSON array of packet id strings.");
            }

            if (ids is null || ids.Length == 0)
                return Results.BadRequest("Provide at least one packet id.");

            var tasks   = Array.ConvertAll(ids, id => SafeGenerateRest(gen, id, ct));
            var results = await Task.WhenAll(tasks);
            return Results.Ok(results);
        });

        // ── Batch by namespace (SSE) ──────────────────────────────────────────
        app.MapPost("/api/generate/by-namespace", async (
            HttpContext http, CodeGenerator gen, IProtocolRepository proto,
            ModelConfigService mcs, CancellationToken ct) =>
        {
            string? ns = null, outputBaseDir = null;
            try
            {
                using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
                ns            = body.RootElement.TryGetProperty("ns",            out var nsEl)  ? nsEl.GetString()  : null;
                outputBaseDir = body.RootElement.TryGetProperty("outputBaseDir", out var dirEl) ? dirEl.GetString() : null;
            }
            catch { http.Response.StatusCode = 400; await http.Response.WriteAsync("Invalid JSON body."); return; }

            if (string.IsNullOrWhiteSpace(ns))
            { http.Response.StatusCode = 400; await http.Response.WriteAsync("Missing 'ns' field."); return; }

            var all = proto.GetPackets();
            if (!all.TryGetValue(ns, out var packets))
            { http.Response.StatusCode = 400; await http.Response.WriteAsync($"Unknown namespace '{ns}'. Valid: {string.Join(", ", all.Keys)}"); return; }

            var ids = packets.Keys.Select(name => $"{ns}.{name}").ToArray();
            if (string.IsNullOrWhiteSpace(outputBaseDir)) outputBaseDir = mcs.Config.OutputBaseDir;
            await BatchSseStreamer.StreamAsync(http, gen, proto, mcs, ids, outputBaseDir, ct);
        });

        // ── Batch by tier (SSE) ───────────────────────────────────────────────
        app.MapPost("/api/generate/by-tier", async (
            HttpContext http, CodeGenerator gen, IProtocolRepository proto,
            ModelConfigService mcs, CancellationToken ct) =>
        {
            string? tier = null, outputBaseDir = null;
            try
            {
                using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
                tier          = body.RootElement.TryGetProperty("tier",          out var tierEl) ? tierEl.GetString() : null;
                outputBaseDir = body.RootElement.TryGetProperty("outputBaseDir", out var dirEl)  ? dirEl.GetString() : null;
            }
            catch { http.Response.StatusCode = 400; await http.Response.WriteAsync("Invalid JSON body."); return; }

            var validTiers = new[] { "tiny", "easy", "medium", "heavy" };
            if (string.IsNullOrWhiteSpace(tier) || !Array.Exists(validTiers, t => t == tier))
            { http.Response.StatusCode = 400; await http.Response.WriteAsync($"Invalid tier. Valid: {string.Join(", ", validTiers)}"); return; }

            var ids = proto.GetPackets()
                .SelectMany(kvp => kvp.Value.Keys.Select(name => $"{kvp.Key}.{name}"))
                .Where(id =>
                {
                    try
                    {
                        var def   = proto.GetPacket(id);
                        var score = PacketComplexityScorer.Compute(def.History);
                        return mcs.ClassifyTier(score).ToLabel() == tier;
                    }
                    catch { return false; }
                })
                .ToArray();

            if (string.IsNullOrWhiteSpace(outputBaseDir)) outputBaseDir = mcs.Config.OutputBaseDir;
            await BatchSseStreamer.StreamAsync(http, gen, proto, mcs, ids, outputBaseDir, ct);
        });

        // ── Batch by explicit ids (SSE) ───────────────────────────────────────
        app.MapPost("/api/generate/batch-ids", async (
            HttpContext http, CodeGenerator gen, IProtocolRepository proto,
            ModelConfigService mcs, CancellationToken ct) =>
        {
            string[]? ids = null;
            string? outputBaseDir = null;
            try
            {
                using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
                if (body.RootElement.TryGetProperty("ids", out var idsEl))
                    ids = idsEl.EnumerateArray().Select(e => e.GetString()!).ToArray();
                if (body.RootElement.TryGetProperty("outputBaseDir", out var dirEl))
                    outputBaseDir = dirEl.GetString();
            }
            catch { http.Response.StatusCode = 400; await http.Response.WriteAsync("Invalid JSON body."); return; }

            if (ids == null || ids.Length == 0)
            { http.Response.StatusCode = 400; await http.Response.WriteAsync("Missing or empty 'ids' array."); return; }

            if (string.IsNullOrWhiteSpace(outputBaseDir)) outputBaseDir = mcs.Config.OutputBaseDir;
            await BatchSseStreamer.StreamAsync(http, gen, proto, mcs, ids, outputBaseDir, ct);
        });
    }

    private static async Task<RestGenerationResult> SafeGenerateRest(CodeGenerator gen, string id, CancellationToken ct)
    {
        try
        {
            return RestGenerationResult.From(await gen.GenerateAsync(id, ct));
        }
        catch (Exception ex)
        {
            return new RestGenerationResult { Name = id, Error = $"{ex.GetType().Name}: {ex.Message}" };
        }
    }
}
