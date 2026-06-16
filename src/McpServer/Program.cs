using System;
using System.Threading.Tasks;
using McpServer;
using McpServer.Endpoints;
using McpServer.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PacketGenerator.Protocol.Loading;

var start = 735;
var end = 772;

Console.WriteLine($"[McpServer] Loading protocols {start}-{end}...");
var repository = await ProtocolDataLoader.LoadRepositoryAsync(new ProtocolDataOptions(start, end));
Console.WriteLine($"[McpServer] Loaded repository for {repository.GetSupportedProtocols()}.");

var builder = WebApplication.CreateBuilder(args);
var port = GetPort(builder.Configuration);

// Logging + Kestrel stay here (builder.WebHost extension not visible outside top-level)
builder.Logging.AddConsole(o => { o.LogToStandardErrorThreshold = LogLevel.Trace; });
builder.WebHost.ConfigureKestrel(o => { o.ListenAnyIP(port); });

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
app.MapGraphApi();
app.MapChunkApi();
app.MapGenerateApi();
app.MapMcp("/mcp");

Console.WriteLine("[McpServer] Ready.");
Console.WriteLine($"[McpServer]   MCP:  http://0.0.0.0:{port}/mcp");
Console.WriteLine($"[McpServer]   UI:   http://localhost:{port}/");

await app.RunAsync();

static int GetPort(IConfiguration configuration)
{
    var raw = configuration["Port"]
              ?? configuration["PORT"]
              ?? Environment.GetEnvironmentVariable("MCP_SERVER_PORT")
              ?? "5000";

    if (int.TryParse(raw, out var port) && port is > 0 and <= 65535)
        return port;

    throw new InvalidOperationException($"Invalid port value '{raw}'. Use --port <1-65535> or MCP_SERVER_PORT.");
}
