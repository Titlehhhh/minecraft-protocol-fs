using System;
using System.IO;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace McpServer.Startup;

public static class ServiceRegistration
{
    public static void AddAppServices(this WebApplicationBuilder builder, IProtocolRepository repository)
    {
        var configKey     = builder.Configuration["OpenRouter:ApiKey"];
        var envKey        = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
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
    }
}
