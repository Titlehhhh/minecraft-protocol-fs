using System;
using System.Threading.Tasks;
using McpServer;
using McpServer.Endpoints;
using McpServer.Repositories;
using McpServer.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

// Logging + Kestrel stay here (builder.WebHost extension not visible outside top-level)
builder.Logging.AddConsole(o => { o.LogToStandardErrorThreshold = LogLevel.Trace; });
builder.WebHost.ConfigureKestrel(o => { o.ListenAnyIP(5000); });

builder.AddAppServices(repository);

var app = builder.Build();

app.UseStaticFiles();
app.MapGet("/", (HttpContext http) =>
{
    http.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

app.MapConfigApi();
app.MapArtifactApi();
app.MapPacketApi();
app.MapGenerateApi();
app.MapMcp("/mcp");

Console.WriteLine("[McpServer] Ready.");
Console.WriteLine("[McpServer]   MCP:  http://0.0.0.0:5000/mcp");
Console.WriteLine("[McpServer]   UI:   http://localhost:5000/");

await app.RunAsync();
