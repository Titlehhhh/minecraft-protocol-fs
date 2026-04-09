using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using McpServer;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ProtoCore;

var start = 735;
var end   = 772;

Console.WriteLine($"[McpServer] Loading protocols {start}–{end}...");
var protocols = await ProtocolLoader.LoadProtocolsAsync(start, end);
Console.WriteLine($"[McpServer] Loaded {protocols.VersionToProtocol.Count} protocol versions.");

var dict = HistoryBuilder.Build(protocols);
Console.WriteLine($"[McpServer] History built: {dict.Count} packet/type entries.");

var repository = new ProtocolRepository(new ProtocolRange(start, end), protocols, dict);

var builder = WebApplication.CreateBuilder(args);

var configKey    = builder.Configuration["OpenRouter:ApiKey"];
var envKey       = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var openRouterKey = configKey
                    ?? envKey
                    ?? throw new InvalidOperationException(
                        "OpenRouter API key not configured. " +
                        "Set OpenRouter:ApiKey in user secrets or OPENROUTER_API_KEY env var.");

Console.WriteLine($"[McpServer] OpenRouter key source: {(configKey != null ? "user secrets" : "env OPENROUTER_API_KEY")}");

var modelConfigFilePath = Path.Combine(AppContext.BaseDirectory, "model-config.json");
var savedConfig         = ModelConfigService.TryLoadFromFile(modelConfigFilePath);
Console.WriteLine(savedConfig is not null
    ? $"[McpServer] Loaded model config from {modelConfigFilePath}"
    : "[McpServer] No saved model config, using defaults");

var modelConfigService = new ModelConfigService(openRouterKey, modelConfigFilePath, savedConfig ?? new ModelConfig());

builder.Logging.AddConsole(o => { o.LogToStandardErrorThreshold = LogLevel.Trace; });
builder.WebHost.ConfigureKestrel(o => { o.ListenAnyIP(5000); });

builder.Services.AddSingleton<IArtifactsRepository>(_ =>
    new FileArtifactsRepository(new ArtifactsOptions
    {
        RootPath = Path.Combine(AppContext.BaseDirectory, "artifacts")
    }));
builder.Services.AddSingleton<IProtocolRepository>(repository);
builder.Services.AddSingleton(modelConfigService);
builder.Services.AddSingleton<StructuralComplexityAssessor>();
builder.Services.AddSingleton<LlmComplexityAssessor>();
builder.Services.AddSingleton<IComplexityAssessor>(sp => sp.GetRequiredService<LlmComplexityAssessor>());
builder.Services.AddSingleton<CodeGenerator>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name        = "McProtoNet",
            Version     = "0.1.0",
            Title       = "Minecraft Protocol Code Generation Server",
            Description = "An MCP server providing discovery, inspection, and code-generation tools for Minecraft protocol packets.",
            WebsiteUrl  = "https://github.com/Titlehhhh/McProtoNet"
        };
    })
    .WithHttpTransport(o => { o.Stateless = true; })
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", (HttpContext http) =>
{
    http.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

// ── Model config ────────────────────────────────────────────────────────────
app.MapGet("/api/config", (ModelConfigService cfg) => Results.Ok(cfg.Config));

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

// ── Artifact download (used by MCP clients) ─────────────────────────────────
app.MapGet("/artifacts/{id}", async (
    string id,
    IArtifactsRepository artifacts,
    HttpContext http,
    CancellationToken ct) =>
{
    var info   = await artifacts.GetInfoAsync(id, ct);
    if (info is null) return Results.NotFound();

    var stream = await artifacts.OpenReadAsync(id, ct);
    if (stream is null) return Results.NotFound();

    http.Response.Headers.ContentDisposition = $"attachment; filename=\"{info.FileName}\"";
    return Results.Stream(stream, info.ContentType);
}).WithName("GetArtifacts");

// ── Packet discovery ─────────────────────────────────────────────────────────
app.MapGet("/api/packets", (IProtocolRepository repo) =>
{
    var result = repo.GetPackets()
        .ToDictionary(kv => kv.Key, kv => kv.Value.Keys.ToArray());
    return Results.Ok(result);
});

app.MapGet("/api/packets/{ns}/{dir}", (string ns, string dir, IProtocolRepository repo) =>
{
    var key = $"{ns}.{dir}";
    var all = repo.GetPackets();
    if (!all.TryGetValue(key, out var packets))
        return Results.NotFound($"Namespace '{key}' not found.");

    var result = packets.Select(kv => new
    {
        Id        = $"{key}.{kv.Key}",
        kv.Value.Name,
        PacketIds = kv.Value.PacketIds.Select(e => new
        {
            From  = e.Range.From,
            To    = e.Range.To,
            HexId = $"0x{e.Id:X2}"
        }).ToArray()
    }).ToArray();

    return Results.Ok(result);
});

// ── Aggregate stats ──────────────────────────────────────────────────────────
app.MapGet("/api/stats", (IProtocolRepository repo) =>
{
    int total = 0, tiny = 0, easy = 0, medium = 0, heavy = 0;
    var byNs      = new SortedDictionary<string, (int Total, int Tiny, int Easy, int Medium, int Heavy)>();
    var perPacket = new List<object>();

    foreach (var (ns, packets) in repo.GetPackets())
    {
        int nsTotal = 0, nsTiny = 0, nsEasy = 0, nsMedium = 0, nsHeavy = 0;
        foreach (var (name, def) in packets)
        {
            var score     = PacketComplexityScorer.Compute(def.History);
            var tierEnum  = modelConfigService.ClassifyTier(score);
            var tierLabel = tierEnum.ToLabel();
            total++; nsTotal++;
            switch (tierEnum)
            {
                case ComplexityTier.Tiny:   tiny++;   nsTiny++;   break;
                case ComplexityTier.Easy:   easy++;   nsEasy++;   break;
                case ComplexityTier.Medium: medium++; nsMedium++; break;
                default:                    heavy++;  nsHeavy++;  break;
            }

            perPacket.Add(new { id = $"{ns}.{name}", score, tier = tierLabel });
        }
        byNs[ns] = (nsTotal, nsTiny, nsEasy, nsMedium, nsHeavy);
    }

    return Results.Ok(new
    {
        total,
        tiers = new { tiny, easy, medium, heavy },
        byNamespace = byNs.Select(kv => new
        {
            ns     = kv.Key,
            total  = kv.Value.Total,
            tiny   = kv.Value.Tiny,
            easy   = kv.Value.Easy,
            medium = kv.Value.Medium,
            heavy  = kv.Value.Heavy
        }).ToArray(),
        packets = perPacket
    });
});

// ── Complexity assessor (without generation) ─────────────────────────────────
app.MapGet("/api/assess/{**id}", async (
    string id,
    IProtocolRepository repo,
    IComplexityAssessor assessor,
    CancellationToken ct) =>
{
    try
    {
        var packet         = repo.GetPacket(id);
        var structuralScore = PacketComplexityScorer.Compute(packet.History);
        var assessment     = await assessor.AssessAsync(packet.History, ct);
        return Results.Ok(new
        {
            id,
            structuralScore,
            tier          = assessment.Tier.ToLabel(),
            llmScore      = assessment.LlmScore,
            reason        = assessment.Reason,
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ── Raw schema viewer ─────────────────────────────────────────────────────────
app.MapGet("/api/schema/{**id}", (string id, IProtocolRepository repo) =>
{
    try
    {
        var packet    = repo.GetPacket(id);
        var supported = repo.GetSupportedProtocols();

        var json = System.Text.Json.JsonSerializer.SerializeToNode(
            packet.History, Protodef.ProtodefType.DefaultJsonOptions)!;
        var obj = json.AsObject();
        PacketPostProcessor.ApplyVersionAliases(obj, supported);

        var jsonStr = System.Text.Json.JsonSerializer.Serialize(json,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var toonStr = Toon.Format.ToonEncoder.EncodeNode(json, new Toon.Format.ToonEncodeOptions());

        var score = PacketComplexityScorer.Compute(packet.History);
        var tier  = modelConfigService.ClassifyTier(score).ToLabel();

        return Results.Ok(new { json = jsonStr, toon = toonStr, complexityScore = score, tier });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
    }
});

// ── Prompt preview (dry-run, no LLM call) ────────────────────────────────────
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

// ── Single generation ─────────────────────────────────────────────────────────
app.MapPost("/api/generate", async (HttpContext http, CodeGenerator gen, CancellationToken ct) =>
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
        outputBaseDir = modelConfigService.Config.OutputBaseDir;

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
            var subdir = ResolvePacketSubdir(id);
            var dir    = Path.Combine(outputBaseDir, subdir);
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

// ── Batch generation ──────────────────────────────────────────────────────────
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

// ── Batch generation (SSE streaming) ─────────────────────────────────────────

app.MapPost("/api/generate/by-namespace", async (HttpContext http, CodeGenerator gen, IProtocolRepository proto, ModelConfigService mcs, CancellationToken ct) =>
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
    await StreamBatchSse(http, gen, proto, mcs, ids, outputBaseDir, ct);
});

app.MapPost("/api/generate/by-tier", async (HttpContext http, CodeGenerator gen, IProtocolRepository proto, ModelConfigService mcs, CancellationToken ct) =>
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
    await StreamBatchSse(http, gen, proto, mcs, ids, outputBaseDir, ct);
});

app.MapPost("/api/generate/batch-ids", async (HttpContext http, CodeGenerator gen, IProtocolRepository proto, ModelConfigService mcs, CancellationToken ct) =>
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
    await StreamBatchSse(http, gen, proto, mcs, ids, outputBaseDir, ct);
});

app.MapMcp("/mcp");

Console.WriteLine("[McpServer] Ready.");
Console.WriteLine("[McpServer]   MCP:  http://0.0.0.0:5000/mcp");
Console.WriteLine("[McpServer]   UI:   http://localhost:5000/");

await app.RunAsync();

static async Task StreamBatchSse(
    HttpContext http, CodeGenerator gen, IProtocolRepository proto,
    ModelConfigService mcs, string[] ids, string? outputBaseDir, CancellationToken ct)
{
    http.Response.ContentType = "text/event-stream; charset=utf-8";
    http.Response.Headers["Cache-Control"] = "no-cache";
    http.Response.Headers["X-Accel-Buffering"] = "no";

    var writeLock = new SemaphoreSlim(1, 1);
    int okCount = 0, errCount = 0;

    async Task WriteSse(object data)
    {
        var json = JsonSerializer.Serialize(data);
        await writeLock.WaitAsync(ct);
        try   { await http.Response.WriteAsync($"data: {json}\n\n", ct); await http.Response.Body.FlushAsync(ct); }
        finally { writeLock.Release(); }
    }

    await WriteSse(new { type = "start", total = ids.Length });

    var semaphores = RestBuildTierSemaphores(mcs.Config);
    try
    {
        var tasks = ids.Select(async id =>
        {
            SemaphoreSlim sem;
            try
            {
                var def  = proto.GetPacket(id);
                var tier = mcs.ClassifyTier(PacketComplexityScorer.Compute(def.History));
                sem = semaphores[tier];
            }
            catch { sem = semaphores[ComplexityTier.Easy]; }

            await sem.WaitAsync(ct);
            try
            {
                string? error = null, model = null, savedTo = null;
                long elapsedMs = 0;
                try
                {
                    var data = await gen.GenerateAsync(id, ct);
                    model = data.Model; elapsedMs = data.ElapsedMs;
                    if (!string.IsNullOrWhiteSpace(outputBaseDir) && !string.IsNullOrWhiteSpace(data.Code))
                    {
                        try
                        {
                            var dir = Path.Combine(outputBaseDir, ResolvePacketSubdir(id));
                            Directory.CreateDirectory(dir);
                            var path = Path.Combine(dir, data.Name + ".cs");
                            await File.WriteAllTextAsync(path, data.Code, ct);
                            savedTo = path;
                        }
                        catch (Exception ex) { Console.Error.WriteLine($"[Batch] Save failed '{id}': {ex.Message}"); }
                    }
                    Interlocked.Increment(ref okCount);
                }
                catch (Exception ex)
                {
                    error = $"{ex.GetType().Name}: {ex.Message}";
                    Interlocked.Increment(ref errCount);
                }
                await WriteSse(new { type = "packet", id, success = error == null, model, elapsedMs, savedTo, error });
            }
            finally { sem.Release(); }
        }).ToArray();

        await Task.WhenAll(tasks);
    }
    catch (OperationCanceledException) { /* client disconnected */ }
    finally
    {
        foreach (var s in semaphores.Values) s.Dispose();
        writeLock.Dispose();
    }

    try { await WriteSse(new { type = "done", total = ids.Length, ok = okCount, err = errCount }); }
    catch { /* client may have disconnected */ }
}

static Dictionary<ComplexityTier, SemaphoreSlim> RestBuildTierSemaphores(ModelConfig cfg) =>
    Enum.GetValues<ComplexityTier>().ToDictionary(t => t, t => new SemaphoreSlim(Math.Max(1, t switch
    {
        ComplexityTier.Tiny   => cfg.Tiny.MaxConcurrency,
        ComplexityTier.Easy   => cfg.Easy.MaxConcurrency,
        ComplexityTier.Medium => cfg.Medium.MaxConcurrency,
        ComplexityTier.Heavy  => cfg.Heavy.MaxConcurrency,
        _                     => 4,
    })));

static async Task<RestGenerationResult> SafeGenerateRest(CodeGenerator gen, string id, CancellationToken ct)
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

static string ResolvePacketSubdir(string id)
{
    var parts = id.Split('.');
    if (parts.Length < 2) return "";
    var ns  = parts[0].ToLowerInvariant();
    var dir = parts[1].ToLowerInvariant();
    var nsName = ns switch
    {
        "play"          => "Play",
        "login"         => "Login",
        "status"        => "Status",
        "configuration" => "Configuration",
        "handshaking"   => "Handshaking",
        _               => ns,
    };
    var dirName = dir switch
    {
        "toclient" => "Clientbound",
        "toserver" => "Serverbound",
        _          => dir,
    };
    return Path.Combine(nsName, dirName);
}
