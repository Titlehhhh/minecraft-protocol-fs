using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using McProtoFacts.Protocol.Queries;
using McProtoFacts.Protocol.Repository;

namespace McpServer.Startup;

public static class ServiceRegistration
{
    public static void AddAppServices(this WebApplicationBuilder builder, IProtocolRepository repository)
    {
        builder.Services.AddSingleton<IProtocolRepository>(repository);
        builder.Services.AddSingleton(sp => new ProtocolQueryService(sp.GetRequiredService<IProtocolRepository>()));
        builder.Services.AddSingleton(sp => new ProtocolUsageQueries(sp.GetRequiredService<IProtocolRepository>()));
        builder.Services.AddSingleton(RagOptions.FromConfiguration(builder.Configuration));
        builder.Services.AddHttpClient<RagEmbeddingClient>();
        builder.Services.AddHttpClient<QdrantChunkStore>();
        builder.Services.AddSingleton<HybridSearchService>();

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name        = "McProtoNet",
                    Version     = "0.1.0",
                    Title       = "Minecraft Protocol Facts Server",
                    Description = "An MCP server providing discovery and inspection tools for Minecraft protocol packets and types.",
                    WebsiteUrl  = "https://github.com/Titlehhhh/McProtoNet"
                };
            })
            .WithHttpTransport(o => { o.Stateless = true; })
            .WithToolsFromAssembly();
    }
}
