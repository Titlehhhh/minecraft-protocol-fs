using System.Linq;
using Humanizer;
using McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoCore;

var protocols = await ProtocolLoader.LoadProtocolsAsync(735, 772);

HistoryBuilder.Build(protocols);




var builder = Host.CreateApplicationBuilder(args);


builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();