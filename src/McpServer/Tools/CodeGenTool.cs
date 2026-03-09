using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CodeGenTool
{
    // Packets with more tokens than this are returned as a prompt for the caller (Claude) to generate
    private const int DirectGenerationThreshold = 4000;

    [McpServerTool(Name = "generate_packet")]
    [Description(
        "Generates a C# packet class from a Minecraft protocol packet identifier. " +
        "Counts the prompt tokens first. " +
        "Simple packets (<=4000 tokens) are generated via the cheap model and saved as an artifact (Name + Link returned). " +
        "Complex packets (>4000 tokens) return TokenCount + SystemPrompt + UserPrompt for the caller to generate.")]
    public static async Task<GenerationResult> GeneratePacket(
        IChatClient chatClient,
        IArtifactsRepository artifacts,
        CodeGenerator codeGenerator,
        IProtocolRepository repository,
        [Description(
            "Packet identifier. Format: '<namespace>.<direction>.<packet_name>'. " +
            "Examples: 'play.toClient.face_player', 'play.toServer.use_item'. " +
            "Do NOT include 'packet_' prefix.")]
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GeneratePacketCore(chatClient, artifacts, codeGenerator, repository, id, cancellationToken);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new McpException($"generate_packet failed for '{id}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<GenerationResult> GeneratePacketCore(
        IChatClient chatClient,
        IArtifactsRepository artifacts,
        CodeGenerator codeGenerator,
        IProtocolRepository repository,
        string id,
        CancellationToken cancellationToken)
    {
        var (system, user, packet) = await codeGenerator.BuildPromptAsync(id, cancellationToken);
        var tokenCount = TokenCounter.Count(system) + TokenCounter.Count(user);

        if (tokenCount > DirectGenerationThreshold)
        {
            return new GenerationResult
            {
                TokenCount = tokenCount,
                SystemPrompt = system,
                UserPrompt = user
            };
        }

        ChatMessage[] messages =
        [
            new(ChatRole.System, system),
            new(ChatRole.User, user)
        ];

        var response = await chatClient.GetResponseAsync(messages, new ChatOptions
        {
            Temperature = 0f,
            MaxOutputTokens = 4096
        }, cancellationToken);

        var rawCode = ExtractCsharpCode(response.Text);
        var supportedRange = repository.GetSupportedProtocols();
        var code = PacketPostProcessor.Process(rawCode, packet, supportedRange);
        var className = BuildClassName(id);

        var artifact = await artifacts.SaveTextAsync(
            className + ".cs",
            code,
            "text/plain; charset=utf-8",
            cancellationToken);

        return new GenerationResult
        {
            Name = className,
            Link = $"/artifacts/{artifact.Id}",
            TokenCount = tokenCount
        };
    }

    private static string ExtractCsharpCode(string text)
    {
        var match = Regex.Match(text, @"```(?:csharp|cs)\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }

    private static string BuildClassName(string id)
    {
        // "play.toServer.use_item" -> "UseItemPacket"
        var lastPart = id.Split('.').Last();
        var withoutPrefix = lastPart.StartsWith("packet_", StringComparison.Ordinal)
            ? lastPart["packet_".Length..]
            : lastPart;
        return withoutPrefix.Pascalize() + "Packet";
    }
}
