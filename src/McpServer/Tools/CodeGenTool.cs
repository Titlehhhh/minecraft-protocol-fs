using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CodeGenTool
{
    [McpServerTool(Name = "generate_packet")]
    [Description(
        "Generates a C# packet class from a Minecraft protocol packet identifier. " +
        "Simple packets (<=4000 tokens) are generated via the cheap model and saved as an artifact (Name + Link returned). " +
        "Complex packets (>4000 tokens) return TokenCount + SystemPrompt + UserPrompt for the caller to generate.")]
    public static async Task<GenerationResult> GeneratePacket(
        CodeGenerator codeGenerator,
        [Description(
            "Packet identifier. Format: '<namespace>.<direction>.<packet_name>'. " +
            "Examples: 'play.toClient.face_player', 'play.toServer.use_item'.")]
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await codeGenerator.GenerateAsync(id, cancellationToken);
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

    [McpServerTool(Name = "generate_packets_batch")]
    [Description(
        "Generates multiple C# packet classes in parallel. " +
        "Returns a list of GenerationResult — same semantics as generate_packet per entry.")]
    public static async Task<List<GenerationResult>> GeneratePacketsBatch(
        CodeGenerator codeGenerator,
        [Description("List of packet identifiers in '<namespace>.<direction>.<name>' format.")]
        string[] ids,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task<GenerationResult>>(ids.Length);
        foreach (var id in ids)
            tasks.Add(GenerateSafe(codeGenerator, id, cancellationToken));

        return [..await Task.WhenAll(tasks)];
    }

    private static async Task<GenerationResult> GenerateSafe(
        CodeGenerator codeGenerator, string id, CancellationToken ct)
    {
        try
        {
            return await codeGenerator.GenerateAsync(id, ct);
        }
        catch (Exception ex)
        {
            return new GenerationResult { Name = id, Error = $"{ex.GetType().Name}: {ex.Message}" };
        }
    }
}