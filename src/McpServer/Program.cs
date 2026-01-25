using System;
using System.ClientModel;
using System.IO;
using System.Linq;
using System.Threading;
using Humanizer;
using McpServer;
using McpServer.Repositories;
using McpServer.Services;
using McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Chat;
using ProtoCore;

var start = 735;
var end = 772;



var protocols = await ProtocolLoader.LoadProtocolsAsync(start, end);

var dict = HistoryBuilder.Build(protocols);

var repository = new ProtocolRepository(new ProtocolRange(start, end), protocols, dict);

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Environment.EnvironmentName);

var openrouterKey = builder.Configuration["OPENROUTER_API_KEY"];


var openAiClient = new OpenAIClient(new ApiKeyCredential(openrouterKey), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://openrouter.ai/api/v1")
});




builder.Services.AddSingleton(openAiClient);

builder.Services.AddSingleton<CodeGenerator>();

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); 
});

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
    .WithHttpTransport()
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

await app.RunAsync();