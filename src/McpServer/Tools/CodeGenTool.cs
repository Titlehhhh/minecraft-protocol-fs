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
        "Complex packets (>threshold tokens) return TokenCount + SystemPrompt + UserPrompt for the caller to generate. " +
        "If OutputBaseDir is configured, the file is also saved to disk automatically (see SavedTo field).")]
    public static async Task<McpGenerationResult> GeneratePacket(
        GenerationService  svc,
        IPacketFileService fileService,
        ModelConfigService mcs,
        [Description(
            "Packet identifier. Format: '<namespace>.<direction>.<packet_name>'. " +
            "Examples: 'play.toClient.face_player', 'play.toServer.use_item'.")]
        string id,
        CancellationToken cancellationToken)
    {
        var result = await svc.GenerateAsync(id, cancellationToken);

        if (!result.IsSuccess)
            throw new McpException(
                $"generate_packet failed for '{id}': [{result.Error!.Kind}] {result.Error.Message}");

        var savedTo = string.IsNullOrWhiteSpace(mcs.Config.OutputBaseDir)
            ? null
            : await fileService.TrySaveAsync(result.Data, id, mcs.Config.OutputBaseDir, cancellationToken);

        return McpGenerationResult.From(result.Data, savedTo);
    }

    [McpServerTool(Name = "generate_packets_batch")]
    [Description(
        "Generates multiple C# packet classes in parallel. " +
        "Returns a list of McpGenerationResult — same semantics as generate_packet per entry. " +
        "Per-packet errors are stored in Error field; the batch does not fail as a whole.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsBatch(
        GenerationService   svc,
        IProtocolRepository protocol,
        IPacketFileService  fileService,
        ModelConfigService  mcs,
        [Description("List of packet identifiers in '<namespace>.<direction>.<name>' format.")]
        string[] ids,
        CancellationToken cancellationToken)
    {
        foreach (var id in ids)
            if (!protocol.ContainsPacket(id))
                throw new McpException($"Unknown packet id '{id}'.");

        return await CollectBatchResults(svc, fileService, mcs, ids, cancellationToken);
    }

    [McpServerTool(Name = "generate_packets_by_tier")]
    [Description(
        "Generates all packets belonging to a specific complexity tier in parallel. " +
        "Values: 'tiny', 'easy', 'medium', 'heavy'. " +
        "Per-packet errors are stored in Error field; the batch does not fail as a whole.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsByTier(
        GenerationService   svc,
        IProtocolRepository protocol,
        IPacketFileService  fileService,
        ModelConfigService  mcs,
        [Description("Complexity tier: 'tiny', 'easy', 'medium', or 'heavy'.")]
        string tier,
        CancellationToken cancellationToken)
    {
        var validTiers = new[] { "tiny", "easy", "medium", "heavy" };
        if (!System.Array.Exists(validTiers, t => t == tier))
            throw new McpException($"Invalid tier '{tier}'. Valid values: tiny, easy, medium, heavy.");

        var ids = protocol.GetPackets()
            .SelectMany(ns => ns.Value.Keys.Select(name => $"{ns.Key}.{name}"))
            .Where(id =>
            {
                var def   = protocol.GetPacket(id);
                var score = PacketComplexityScorer.Compute(def.History);
                return mcs.ClassifyTier(score).ToLabel() == tier;
            })
            .ToArray();

        return await CollectBatchResults(svc, fileService, mcs, ids, cancellationToken);
    }

    [McpServerTool(Name = "generate_packets_by_namespace")]
    [Description(
        "Generates all packets in a given namespace+direction in parallel. " +
        "Examples: 'play.toClient', 'play.toServer', 'login.toClient', 'configuration.toServer'. " +
        "Per-packet errors are stored in Error field; the batch does not fail as a whole.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsByNamespace(
        GenerationService   svc,
        IProtocolRepository protocol,
        IPacketFileService  fileService,
        ModelConfigService  mcs,
        [Description("Namespace + direction, e.g. 'play.toClient' or 'login.toServer'.")]
        string ns,
        CancellationToken cancellationToken)
    {
        var all = protocol.GetPackets();
        if (!all.TryGetValue(ns, out var packets))
            throw new McpException($"Unknown namespace '{ns}'. Valid values: {string.Join(", ", all.Keys)}");

        var ids = packets.Keys.Select(name => $"{ns}.{name}").ToArray();
        return await CollectBatchResults(svc, fileService, mcs, ids, cancellationToken);
    }

    // ── Shared batch collector — одно место, используется всеми MCP-батч-тулзами ──
    private static async Task<List<McpGenerationResult>> CollectBatchResults(
        GenerationService  svc,
        IPacketFileService fileService,
        ModelConfigService mcs,
        string[]           ids,
        CancellationToken  ct)
    {
        var results = new List<McpGenerationResult>(ids.Length);

        await foreach (var r in svc.GenerateBatchAsync(ids, ct))
        {
            string? savedTo = null;
            if (r.IsSuccess && !string.IsNullOrWhiteSpace(mcs.Config.OutputBaseDir))
                savedTo = await fileService.TrySaveAsync(r.Data, r.Id, mcs.Config.OutputBaseDir, ct);

            results.Add(r.IsSuccess
                ? McpGenerationResult.From(r.Data, savedTo)
                : new McpGenerationResult
                {
                    Name  = r.Id,
                    Error = $"[{r.Error!.Kind}] {r.Error.Message}",
                });
        }

        return results;
    }
}
