using System;
using System.ClientModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        [Description(
            "Optional. Base directory for the McProtoNet.Protocol Packets folder " +
            "(e.g. 'C:/repo/McProtoNet/src/McProtoNet.Protocol/Packets'). " +
            "When provided, the generated file is saved automatically to the correct subdirectory " +
            "(e.g. Play/Clientbound/HurtAnimationPacket.cs). No curl needed.")]
        string? outputBaseDir,
        CancellationToken cancellationToken)
    {
        try
        {
            var data    = await codeGenerator.GenerateAsync(id, cancellationToken);
            var savedTo = TrySaveToDir(data, id, outputBaseDir);
            return McpGenerationResult.From(data, savedTo);
        }
        catch (McpException)
        {
            throw;
        }
        catch (ClientResultException ex)
        {
            var body = ex.GetRawResponse()?.Content.ToString();
            var detail = string.IsNullOrWhiteSpace(body)
                ? ex.Message
                : $"{ex.Message} | Response: {body}";
            throw new McpException($"generate_packet failed for '{id}': HTTP {ex.Status}: {detail}");
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
        ModelConfigService modelConfig,
        [Description("List of packet identifiers in '<namespace>.<direction>.<name>' format.")]
        string[] ids,
        [Description("Optional. Base Packets directory. Files are saved automatically to subdirectories (e.g. Play/Clientbound/).")]
        string? outputBaseDir,
        CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            if (!protocol.ContainsPacket(id))
                throw new McpException($"Unknown packet id '{id}'.");
        }

        var semaphores = BuildTierSemaphores(modelConfig.Config);
        try
        {
            Task<McpGenerationResult>[] tasks = Array.ConvertAll(ids, id =>
            {
                var def  = protocol.GetPacket(id);
                var tier = modelConfig.ClassifyTier(PacketComplexityScorer.Compute(def.History));
                return GenerateThrottled(codeGenerator, semaphores[tier], id, outputBaseDir, cancellationToken);
            });
            return new List<McpGenerationResult>(await Task.WhenAll(tasks));
        }
        finally
        {
            foreach (var s in semaphores.Values) s.Dispose();
        }
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
        [Description("Optional. Base Packets directory. Files are saved automatically to subdirectories (e.g. Play/Clientbound/).")]
        string? outputBaseDir,
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

        var parsedTier = Enum.GetValues<ComplexityTier>().First(t => t.ToLabel() == tier);
        var semaphore  = new SemaphoreSlim(TierConcurrency(parsedTier, modelConfig.Config));
        try
        {
            Task<McpGenerationResult>[] tasks = Array.ConvertAll(ids, id => GenerateThrottled(codeGenerator, semaphore, id, outputBaseDir, cancellationToken));
            return new List<McpGenerationResult>(await Task.WhenAll(tasks));
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    [McpServerTool(Name = "generate_packets_by_namespace")]
    [Description(
        "Generates all packets in a given namespace+direction in parallel. " +
        "Examples: 'play.toClient', 'play.toServer', 'login.toClient', 'configuration.toServer'. " +
        "Returns a list of McpGenerationResult. Per-packet errors are stored in Error field.")]
    public static async Task<List<McpGenerationResult>> GeneratePacketsByNamespace(
        CodeGenerator codeGenerator,
        IProtocolRepository protocol,
        ModelConfigService modelConfig,
        [Description("Namespace + direction, e.g. 'play.toClient' or 'login.toServer'.")]
        string ns,
        [Description("Optional. Base Packets directory. Files are saved automatically to subdirectories (e.g. Play/Clientbound/).")]
        string? outputBaseDir,
        CancellationToken cancellationToken)
    {
        var all = protocol.GetPackets();
        if (!all.TryGetValue(ns, out var packets))
            throw new McpException($"Unknown namespace '{ns}'. Valid values: {string.Join(", ", all.Keys)}");

        var ids = packets.Keys.Select(name => $"{ns}.{name}").ToArray();

        var semaphores = BuildTierSemaphores(modelConfig.Config);
        try
        {
            Task<McpGenerationResult>[] tasks = Array.ConvertAll(ids, id =>
            {
                var def  = protocol.GetPacket(id);
                var tier = modelConfig.ClassifyTier(PacketComplexityScorer.Compute(def.History));
                return GenerateThrottled(codeGenerator, semaphores[tier], id, outputBaseDir, cancellationToken);
            });
            return new List<McpGenerationResult>(await Task.WhenAll(tasks));
        }
        finally
        {
            foreach (var s in semaphores.Values) s.Dispose();
        }
    }

    private static Dictionary<ComplexityTier, SemaphoreSlim> BuildTierSemaphores(ModelConfig cfg) =>
        Enum.GetValues<ComplexityTier>().ToDictionary(t => t, t => new SemaphoreSlim(TierConcurrency(t, cfg)));

    private static int TierConcurrency(ComplexityTier tier, ModelConfig cfg) =>
        Math.Max(1, tier switch
        {
            ComplexityTier.Tiny   => cfg.Tiny.MaxConcurrency,
            ComplexityTier.Easy   => cfg.Easy.MaxConcurrency,
            ComplexityTier.Medium => cfg.Medium.MaxConcurrency,
            ComplexityTier.Heavy  => cfg.Heavy.MaxConcurrency,
            _                     => 4,
        });

    private static async Task<McpGenerationResult> GenerateThrottled(
        CodeGenerator codeGenerator, SemaphoreSlim semaphore, string id, string? outputBaseDir, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            return await GenerateSafe(codeGenerator, id, outputBaseDir, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<McpGenerationResult> GenerateSafe(
        CodeGenerator codeGenerator, string id, string? outputBaseDir, CancellationToken ct)
    {
        const int maxAttempts = 4;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var data    = await codeGenerator.GenerateAsync(id, ct);
                var savedTo = TrySaveToDir(data, id, outputBaseDir);
                return McpGenerationResult.From(data, savedTo);
            }
            catch (ClientResultException ex) when (IsRetryable(ex) && attempt < maxAttempts)
            {
                await Task.Delay(attempt * 2000, ct);
            }
            catch (ClientResultException ex)
            {
                var body = ex.GetRawResponse()?.Content.ToString();
                var detail = string.IsNullOrWhiteSpace(body)
                    ? ex.Message
                    : $"{ex.Message} | Response: {body}";
                return new McpGenerationResult { Name = id, Error = $"HTTP {ex.Status}: {detail}" };
            }
            catch (Exception ex)
            {
                return new McpGenerationResult { Name = id, Error = $"{ex.GetType().Name}: {ex.Message}" };
            }
        }
        return new McpGenerationResult { Name = id, Error = "Max retries exceeded" };
    }

    private static string? TrySaveToDir(GenerationData data, string id, string? outputBaseDir)
    {
        if (string.IsNullOrWhiteSpace(outputBaseDir) || string.IsNullOrWhiteSpace(data.Code))
            return null;
        try
        {
            var subdir = ResolveSubdir(id);
            var dir    = Path.Combine(outputBaseDir, subdir);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, data.Name + ".cs");
            File.WriteAllText(path, data.Code);
            return path;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static string ResolveSubdir(string id)
    {
        // id format: "namespace.direction.name"  e.g. "play.toClient.hurt_animation"
        var parts = id.Split('.');
        if (parts.Length < 2) return "";
        var ns  = parts[0].ToLowerInvariant();
        var dir = parts[1].ToLowerInvariant();
        var nsName = ns switch
        {
            "play"          => "Play",
            "login"         => "Login",
            "status"        => "Status",
            "configuration" => "Configuration",
            "handshaking"   => "Handshaking",
            _               => ns,
        };
        var dirName = dir switch
        {
            "toclient" => "Clientbound",
            "toserver" => "Serverbound",
            _          => dir,
        };
        return Path.Combine(nsName, dirName);
    }

    private static bool IsRetryable(ClientResultException ex)
    {
        if (ex.Status is 429 or 503 or 502) return true;
        if (ex.Status == 400)
        {
            var body = ex.GetRawResponse()?.Content.ToString() ?? "";
            return body.Contains("Context size", StringComparison.OrdinalIgnoreCase)
                || body.Contains("context_length", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
