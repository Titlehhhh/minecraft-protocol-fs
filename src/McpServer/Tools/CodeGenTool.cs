using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Repositories;
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
        "Simple packets are generated via LLM and saved as an artifact — returns Name + Link (download URL). " +
        "Use the link to curl the file to disk. Read 1-2 files to verify quality. " +
        "Complex packets (>threshold tokens) return TokenCount + SystemPrompt + UserPrompt for the caller to generate.")]
    public static async Task<McpGenerationResult> GeneratePacket(
        CodeGenerator codeGenerator,
        [Description(
            "Packet identifier. Format: '<namespace>.<direction>.<packet_name>'. " +
            "Examples: 'play.toClient.face_player', 'play.toServer.use_item'.")]
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            return McpGenerationResult.From(await codeGenerator.GenerateAsync(id, cancellationToken));
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
        "Returns a list of McpGenerationResult — same semantics as generate_packet per entry. " +
        "Per-packet errors are stored in Error field; the batch does not fail as a whole.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsBatch(
        CodeGenerator codeGenerator,
        IProtocolRepository protocol,
        [Description("List of packet identifiers in '<namespace>.<direction>.<name>' format.")]
        string[] ids,
        CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            if (!protocol.ContainsPacket(id))
                throw new McpException($"Unknown packet id '{id}'.");
        }

        Task<McpGenerationResult>[] tasks = Array.ConvertAll(ids, id => GenerateSafe(codeGenerator, id, cancellationToken));
        return new List<McpGenerationResult>(await Task.WhenAll(tasks));
    }

    [McpServerTool(Name = "generate_packets_by_tier")]
    [Description(
        "Generates all packets belonging to a specific complexity tier in parallel. " +
        "Tier is determined by current model config thresholds. " +
        "Values: 'tiny' (local model), 'easy', 'medium', 'heavy'. " +
        "Returns a list of McpGenerationResult — same semantics as generate_packet per entry. " +
        "Per-packet errors are stored in Error field; the batch does not fail as a whole. " +
        "Use this to bulk-generate all simple/tiny packets without listing them manually.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsByTier(
        CodeGenerator codeGenerator,
        IProtocolRepository protocol,
        ModelConfigService modelConfig,
        [Description("Complexity tier: 'tiny', 'easy', 'medium', or 'heavy'.")]
        string tier,
        CancellationToken cancellationToken)
    {
        var validTiers = new[] { "tiny", "easy", "medium", "heavy" };
        if (!Array.Exists(validTiers, t => t == tier))
            throw new McpException($"Invalid tier '{tier}'. Valid values: tiny, easy, medium, heavy.");

        var ids = protocol.GetPackets()
            .SelectMany(ns => ns.Value.Keys.Select(name => $"{ns.Key}.{name}"))
            .Where(id =>
            {
                var def   = protocol.GetPacket(id);
                var score = PacketComplexityScorer.Compute(def.History);
                return modelConfig.ClassifyTier(score).ToLabel() == tier;
            })
            .ToArray();

        Task<McpGenerationResult>[] tasks = Array.ConvertAll(ids, id => GenerateSafe(codeGenerator, id, cancellationToken));
        return new List<McpGenerationResult>(await Task.WhenAll(tasks));
    }

    private static async Task<McpGenerationResult> GenerateSafe(
        CodeGenerator codeGenerator, string id, CancellationToken ct)
    {
        try
        {
            return McpGenerationResult.From(await codeGenerator.GenerateAsync(id, ct));
        }
        catch (Exception ex)
        {
            return new McpGenerationResult { Name = id, Error = $"{ex.GetType().Name}: {ex.Message}" };
        }
    }
}
