using System;
using System.ClientModel;
using System.IO;
using System.Threading;
using McpServer;
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

builder.Services.AddSingleton<CodeGenerator>();

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

builder.Services.AddSingleton<IArtifactsRepository>(sp =>
{
    var options = new ArtifactsOptions
    {
        RootPath = Path.Combine(AppContext.BaseDirectory, "artifacts")
    };

    return new FileArtifactsRepository(options);
});

builder.Services.AddSingleton<IProtocolRepository>(repository);

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
                "Minecraft protocol types and packets. " +
                "The server is designed to support code generation, validation, and " +
                "versioned protocol updates, with MCP used as an orchestration layer " +
                "rather than a primary data transport.",
            WebsiteUrl = "https://github.com/Titlehhhh/McProtoNet"
        };
    })
    .WithHttpTransport(gg => { gg.Stateless = true; })
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/artifacts/{id}", async (
    string id,
    IArtifactsRepository artifacts,
    HttpContext http,
    CancellationToken ct) =>
{
    var info = await artifacts.GetInfoAsync(id, ct);
    if (info is null)
        return Results.NotFound();

    var stream = await artifacts.OpenReadAsync(id, ct);
    if (stream is null)
        return Results.NotFound();

    http.Response.Headers.ContentDisposition =
        $"attachment; filename=\"{info.FileName}\"";

    return Results.Stream(stream, info.ContentType);
}).WithName("GetArtifacts");
app.MapMcp("/mcp");

Console.WriteLine("[McpServer] Ready. Listening on http://0.0.0.0:5000/mcp");

await app.RunAsync();