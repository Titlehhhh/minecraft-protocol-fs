using System.Linq;
using Humanizer;
using McpServer;
using McpServer.Repositories;
using McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoCore;

var start = 735;
var end = 772;

var protocols = await ProtocolLoader.LoadProtocolsAsync(start, end);

var dict = HistoryBuilder.Build(protocols);

var repository = new ProtocolRepository(new ProtocolRange(start, end), protocols, dict);

var builder = WebApplication.CreateBuilder(args);


builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IProtocolRepository>(repository);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var webApp = builder.Build();

webApp.MapMcp("/mcp");
await webApp.RunAsync();

