using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Repositories;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using Protodef;
using Toon.Format;

namespace McpServer.Tools;

[McpServerToolType]
public static class DataTool
{
    [McpServerTool, Description("All types")]
    public static string GetTypes(IProtocolRepository repository)
    {
        return string.Join(", ", repository.GetTypes());
    }

    [McpServerTool, Description("All packets")]
    public static string GetPackets(
        IProtocolRepository repository,
        [Description("Regex filter, optional")]
        string? filter = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return string.Join(", ", repository.GetPackets());
        }

        var regex = new Regex(filter);
        var packets = repository.GetPackets().Where(x => regex.IsMatch(x));
        return string.Join(", ", packets);
    }

    [McpServerTool(UseStructuredContent = false), Description("Get type from id")]
    public static string GetType(
        IProtocolRepository repository,
        string id,
        [Description("Format: toon(optimized, use) or json")]
        string format = "toon")
    {
        var hist = repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(hist, ProtodefType.DefaultJsonOptions);

        if (format == "toon")
        {
            return ToonEncoder.EncodeNode(json, new ToonEncodeOptions());
        }

        return json.ToJsonString(new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }
}

public class GenerationResult
{
    public string Name { get; set; }
    public string Link { get; set; }
}

[McpServerToolType]
public static class CodeGenTool
{
    [McpServerTool]
    public static async Task<GenerationResult> GeneratePacket(
        ModelContextProtocol.Server.McpServer thisServer,
        IProtocolRepository repository,
        string id,
        CancellationToken cancellationToken)
    {
        var history = repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(history, ProtodefType.DefaultJsonOptions)!;
        var toon = ToonEncoder.EncodeNode(json, new ToonEncodeOptions());


        var prompt = new StringBuilder();

        prompt.AppendLine("Packet type specification:");
        prompt.AppendLine();
        prompt.AppendLine(toon);
        prompt.AppendLine();
        prompt.AppendLine("Generate C# packet code according to the rules.");

        ChatMessage[] messages =
        [
            new(
                ChatRole.System,
                """
                You are a strict C# code generator for Minecraft protocol packets.

                Rules:
                - Output ONLY valid C# code
                - Do NOT include explanations or markdown
                - Do NOT invent fields
                - Follow the provided specification exactly
                """
            ),
            new(
                ChatRole.User,
                prompt.ToString()
            )
        ];

        var chat = thisServer.AsSamplingChatClient();

        var response = await chat.GetResponseAsync(
            messages,
            new ChatOptions
            {
                Temperature = 0,
                MaxOutputTokens = 2000,
            },
            cancellationToken);

        return new GenerationResult()
        {
            Link = "test",
            Name = "test"
        };
    }
}

[McpServerResourceType]
public static class Resources
{
}