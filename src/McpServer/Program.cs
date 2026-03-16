using System;
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
    Console.WriteLine($"[McpServer] Config updated: small={updated.SmallModel} medium={updated.MediumModel} heavy={updated.HeavyModel} thresh={updated.SmallThreshold}/{updated.HeavyThreshold}");
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

// ── Raw schema viewer ─────────────────────────────────────────────────────────
app.MapGet("/api/schema/{**id}", (string id, IProtocolRepository repo) =>
{
    try
    {
        var packet = repo.GetPacket(id);
        var supported = repo.GetSupportedProtocols();
        var first = supported.From.ToString();
        var last  = supported.To.ToString();

        var json = System.Text.Json.JsonSerializer.SerializeToNode(
            packet.History, Protodef.ProtodefType.DefaultJsonOptions)!;
        var obj = json.AsObject();
        // Replace version numbers with first/last aliases
        for (var i = 0; i < obj.Count; i++)
        {
            var node   = obj.GetAt(i);
            var newKey = node.Key.Replace(first, "first").Replace(last, "last");
            if (newKey != node.Key)
                obj.SetAt(i, newKey, node.Value?.DeepClone());
        }

        var jsonStr = System.Text.Json.JsonSerializer.Serialize(json,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var toonStr = Toon.Format.ToonEncoder.EncodeNode(json, new Toon.Format.ToonEncodeOptions());

        return Results.Ok(new { json = jsonStr, toon = toonStr });
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
        return Results.Ok(new { system, user, tokenCount = TokenCounter.Count(system, user) });
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

    return Results.Ok(RestGenerationResult.From(data));
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

app.MapMcp("/mcp");

Console.WriteLine("[McpServer] Ready.");
Console.WriteLine("[McpServer]   MCP:  http://0.0.0.0:5000/mcp");
Console.WriteLine("[McpServer]   UI:   http://localhost:5000/");

await app.RunAsync();

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
