using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Models;
using McpServer.Services;
using ModelContextProtocol.Server;
using PacketGenerator.Protocol.Complexity;
using PacketGenerator.Protocol.Queries;
using PacketGenerator.Protocol.Repository;
using PacketGenerator.Protocol.Serialization;
using Protodef;

namespace McpServer.Tools;

public enum PacketFilterMode
{
    Contains,
    StartsWith,
    Exact
}

[McpServerToolType]
public static class DataTool
{
    [McpServerTool]
    [Description(
        "Returns a list of all known protocol type identifiers. " +
        "Each identifier uniquely represents a data type defined in the protocol. " +
        "The result is intended for discovery and inspection, not for bulk data transfer."
    )]
    public static string GetTypes(IProtocolRepository repository)
    {
        return string.Join(", ", repository.GetTypes());
    }

    [McpServerTool]
    [Description(
        "Returns a list of all known packet identifiers.\n\n" +
        "Text filtering (filter parameter):\n" +
        "- Plain text, case-insensitive, NOT a regular expression.\n" +
        "- Multiple tokens separated by '|' — packet must contain ALL tokens.\n" +
        "- Example: filter=\"player|move\" → matches play.toServer.player_move\n\n" +
        "Complexity filtering (tier parameter):\n" +
        "- Values: 'easy', 'medium', 'heavy'.\n" +
        "- Filters by structural complexity tier based on current model config thresholds.\n" +
        "- Use 'easy' to get simple packets safe to generate with cheap models.\n" +
        "- Use 'heavy' to find packets that require special handling or Claude-level reasoning.\n" +
        "- Both filters can be combined.\n\n" +
        "Do NOT use wildcards or regex in the filter parameter."
    )]
    public static string GetPackets(
        IProtocolRepository repository,
        ModelConfigService modelConfig,
        string? filter = null,
        [Description("Optional complexity tier filter: 'tiny', 'easy', 'medium', or 'heavy'. Leave null to return all tiers.")]
        string? tier = null)
    {
        // Apply complexity tier filter if requested
        Func<string, string[], string[]> applyTier = tier is null
            ? (_, names) => names
            : (ns, names) => names.Where(name =>
              {
                  var def   = repository.GetPacket($"{ns}.{name}");
                  var score = PacketComplexityScorer.Compute(def.History);
                  return modelConfig.ClassifyTier(score).ToLabel() == tier;
              }).ToArray();

        var packets =
            repository.GetPackets()
                .Select(x =>
                {
                    var filtered = applyTier(x.Key, x.Value.Keys.ToArray());
                    return new KeyValuePair<string, string[]>(x.Key, filtered);
                })
                .Where(x => x.Value.Length > 0)
                .ToDictionary();

        if (string.IsNullOrWhiteSpace(filter))
        {
            var json = JsonSerializer.SerializeToNode(packets, ProtodefType.DefaultJsonOptions);
            return json.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        else
        {
            static string Normalize(string value)
            {
                value = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");

                return new string(
                    value
                        .Where(char.IsLetterOrDigit)
                        .Select(char.ToLowerInvariant)
                        .ToArray()
                );
            }

            var tokens = filter
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .ToArray();

            packets = packets.Select(packetList =>
                {
                    var filtered = packetList.Value.Where(p =>
                    {
                        return tokens.All(t => p.Contains(t, StringComparison.OrdinalIgnoreCase));
                    }).ToArray();

                    return new KeyValuePair<string, string[]>(packetList.Key, filtered);
                })
                .Where(x => x.Value.Any())
                .ToDictionary();

            var json = JsonSerializer.SerializeToNode(packets, ProtodefType.DefaultJsonOptions);
            return json.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns the full versioned definition of a specific packet. " +
        "The id format is 'namespace.packetName', e.g. 'play.toClient.keep_alive'. " +
        "Use get_packets to discover valid identifiers."
    )]
    public static string GetPacket(
        IProtocolRepository repository,
        string id,
        string format = "toon")
    {
        var def = repository.GetPacket(id);
        var hist = new TypeHistory
        {
            Name = def.Name,
            Id = id,
        };
        foreach (var kv in def.History)
            hist.History[kv.Key] = kv.Value;

        var json = JsonSerializer.SerializeToNode(hist, ProtodefType.DefaultJsonOptions);

        if (format == "toon") return ToonSerializer.Encode(json);

        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns the full versioned definition of a protocol type (or packet) identified by its id. " +
        "The definition includes structural changes across protocol versions. " +
        "The result is returned as formatted text for inspection or analysis, " +
        "not as structured data for further automated processing."
    )]
    public static string GetType(
        IProtocolRepository repository,
        string id,
        [Description(
            "Output format of the type definition. " +
            "Use 'toon' (default) for a compact, optimized, human-readable format suitable for LLM inspection. " +
            "Use 'json' for a fully expanded JSON representation intended for debugging or manual review."
        )]
        string format = "toon")
    {
        var hist = repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(hist, ProtodefType.DefaultJsonOptions);

        if (format == "toon") return ToonSerializer.Encode(json);

        return json.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns compact usage statistics for protocol packets, named types, native types, and protodef shapes. " +
        "Use kind='packet', 'type', 'native', or 'shape' to filter; use top to keep output small."
    )]
    public static string GetProtocolUsage(
        ProtocolUsageQueries usage,
        int? top = 25,
        string? kind = null,
        string format = "json")
    {
        return SerializeUsage(usage.GetUsage(top, kind), format);
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns where a packet, type, native type, or protodef shape is used. " +
        "Accepts ids like play.toServer.window_click, HashedSlot, type:HashedSlot, native:varint, or shape:container."
    )]
    public static string GetProtocolUsers(
        ProtocolUsageQueries usage,
        string id,
        string format = "toon")
    {
        return SerializeUsage(usage.GetUsers(id), format);
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns compact dependencies used by a packet or protocol type, including target path, version ranges, and field paths."
    )]
    public static string GetProtocolDependencies(
        ProtocolUsageQueries usage,
        string id,
        string format = "toon")
    {
        return SerializeUsage(usage.GetDependencies(id), format);
    }

    private static string SerializeUsage<T>(T value, string format)
    {
        var json = JsonSerializer.SerializeToNode(value, ProtodefType.DefaultJsonOptions);
        if (format == "toon") return ToonSerializer.Encode(json);
        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}

[McpServerResourceType]
public static class Resources
{
}
