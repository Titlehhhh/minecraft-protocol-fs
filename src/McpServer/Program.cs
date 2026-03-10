using System;
using System.ClientModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using OpenAI;
using ProtoCore;

var start = 735;
var end = 772;

Console.WriteLine($"[McpServer] Loading protocols {start}–{end}...");
var protocols = await ProtocolLoader.LoadProtocolsAsync(start, end);
Console.WriteLine($"[McpServer] Loaded {protocols.VersionToProtocol.Count} protocol versions.");

var dict = HistoryBuilder.Build(protocols);
Console.WriteLine($"[McpServer] History built: {dict.Count} packet/type entries.");

var repository = new ProtocolRepository(new ProtocolRange(start, end), protocols, dict);

var builder = WebApplication.CreateBuilder(args);

var configKey = builder.Configuration["OpenRouter:ApiKey"];
var envKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var openRouterKey = configKey
                    ?? envKey
                    ?? throw new InvalidOperationException(
                        "OpenRouter API key not configured. " +
                        "Set OpenRouter:ApiKey in user secrets or OPENROUTER_API_KEY env var.");

var cheapModel = builder.Configuration["OpenRouter:CheapModel"] ?? "openai/gpt-4o-mini";
var keySource = configKey != null ? "user secrets" : "env OPENROUTER_API_KEY";
Console.WriteLine($"[McpServer] OpenRouter key source: {keySource}");
Console.WriteLine($"[McpServer] Cheap model: {cheapModel}");

builder.Services.AddChatClient(
    new OpenAIClient(
        new ApiKeyCredential(openRouterKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1/") }
    ).GetChatClient(cheapModel).AsIChatClient());

builder.Logging.AddConsole(consoleLogOptions => { consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace; });

builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(5000); });

builder.Services.AddSingleton<IArtifactsRepository>(_ =>
    new FileArtifactsRepository(new ArtifactsOptions
    {
        RootPath = Path.Combine(AppContext.BaseDirectory, "artifacts")
    }));

builder.Services.AddSingleton<IProtocolRepository>(repository);
builder.Services.AddSingleton<CodeGenerator>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "McProtoNet",
            Version = "0.1.0",
            Title = "Minecraft Protocol Code Generation Server",
            Description =
                "An MCP server providing discovery, inspection, and maintenance tools for " +
                "Minecraft protocol types and packets.",
            WebsiteUrl = "https://github.com/Titlehhhh/McProtoNet"
        };
    })
    .WithHttpTransport(gg => { gg.Stateless = true; })
    .WithToolsFromAssembly();

var app = builder.Build();

// ── artifacts download ─────────────────────────────────────────────────────
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

// ── REST: single generation ────────────────────────────────────────────────
// POST /api/generate  body: { "id": "play.toServer.use_item" }
// Returns GenerationResult as JSON (or raw .cs if Accept: text/plain)
app.MapPost("/api/generate", async (
    HttpContext http,
    CodeGenerator codeGenerator,
    CancellationToken ct) =>
{
    using var body = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct);
    if (!body.RootElement.TryGetProperty("id", out var idEl))
        return Results.BadRequest("Missing 'id' field.");

    var id = idEl.GetString();
    if (string.IsNullOrWhiteSpace(id))
        return Results.BadRequest("'id' must not be empty.");

    var result = await codeGenerator.GenerateAsync(id, ct);

    // If caller wants raw code (curl ... > MyPacket.cs), fetch artifact content directly
    if (http.Request.Headers.Accept.ToString().Contains("text/plain") && result.Link is not null)
    {
        var artifactId = result.Link.TrimStart('/').Replace("artifacts/", "");
        var artifacts = http.RequestServices.GetRequiredService<IArtifactsRepository>();
        var stream = await artifacts.OpenReadAsync(artifactId, ct);
        if (stream is not null)
            return Results.Stream(stream, "text/plain; charset=utf-8",
                fileDownloadName: result.Name + ".cs");
    }

    return Results.Ok(result);
});

// ── REST: batch generation ─────────────────────────────────────────────────
// POST /api/generate/batch  body: ["play.toServer.use_item", "play.toClient.face_player"]
app.MapPost("/api/generate/batch", async (
    HttpContext http,
    CodeGenerator codeGenerator,
    CancellationToken ct) =>
{
    string[]? ids;
    try
    {
        ids = await JsonSerializer.DeserializeAsync<string[]>(http.Request.Body,
            cancellationToken: ct);
    }
    catch
    {
        return Results.BadRequest("Body must be a JSON array of packet id strings.");
    }

    if (ids is null || ids.Length == 0)
        return Results.BadRequest("Provide at least one packet id.");

    var tasks = Array.ConvertAll(ids, id => SafeGenerate(codeGenerator, id, ct));
    var results = await Task.WhenAll(tasks);
    return Results.Ok(results);
});

app.MapMcp("/mcp");

static async Task<GenerationResult> SafeGenerate(CodeGenerator gen, string id, CancellationToken ct)
{
    try { return await gen.GenerateAsync(id, ct); }
    catch (Exception ex) { return new GenerationResult { Name = id, Error = $"{ex.GetType().Name}: {ex.Message}" }; }
}

Console.WriteLine("[McpServer] Ready. Listening on http://0.0.0.0:5000/mcp");

await app.RunAsync();
