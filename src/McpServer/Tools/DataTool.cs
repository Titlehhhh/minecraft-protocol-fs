using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PacketGenerator.Protocol.Queries;
using PacketGenerator.Protocol.Serialization;
using Protodef;

namespace McpServer.Tools;

[McpServerToolType]
public static class DataTool
{
    [McpServerTool]
    [Description(
        "Returns a list of all known protocol type identifiers. " +
        "Each identifier uniquely represents a data type defined in the protocol. " +
        "The result is intended for discovery and inspection, not for bulk data transfer."
    )]
    public static string GetTypes(ProtocolQueryService query)
    {
        return string.Join(", ", query.GetTypes());
    }

    [McpServerTool]
    [Description(
        "Returns a list of all known packet identifiers.\n\n" +
        "Text filtering (filter parameter):\n" +
        "- Plain text, case-insensitive, NOT a regular expression.\n" +
        "- Multiple tokens separated by '|' — packet id must contain ALL tokens.\n" +
        "- Example: filter=\"player|move\" → matches play.toServer.player_move\n\n" +
        "Complexity filtering (tier parameter):\n" +
        "- Values: 'tiny', 'easy', 'medium', 'heavy'.\n" +
        "- Filters by structural complexity tier.\n" +
        "- Use 'easy' to get structurally simple packets.\n" +
        "- Use 'heavy' to find packets with the most complex wire structure.\n" +
        "- Both filters can be combined.\n\n" +
        "Do NOT use wildcards or regex in the filter parameter."
    )]
    public static string GetPackets(
        ProtocolQueryService query,
        string? filter = null,
        [Description("Optional complexity tier filter: 'tiny', 'easy', 'medium', or 'heavy'. Leave null to return all tiers.")]
        string? tier = null)
    {
        var json = JsonSerializer.SerializeToNode(query.GetPackets(filter, tier), ProtodefType.DefaultJsonOptions);
        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns the full versioned definition of a specific packet. " +
        "The id format is 'namespace.packetName', e.g. 'play.toClient.keep_alive'. " +
        "Use get_packets to discover valid identifiers."
    )]
    public static string GetPacket(
        ProtocolQueryService query,
        string id,
        string format = "toon")
    {
        return query.GetPacketSchema(id, ParseFormat(format)).Schema;
    }

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Returns the full versioned definition of a protocol type identified by its id. " +
        "The definition includes structural changes across protocol versions. " +
        "The result is returned as formatted text for inspection or analysis, " +
        "not as structured data for further automated processing."
    )]
    public static string GetType(
        ProtocolQueryService query,
        string id,
        [Description(
            "Output format of the type definition. " +
            "Use 'toon' (default) for a compact, optimized, human-readable format suitable for LLM inspection. " +
            "Use 'json' for a fully expanded JSON representation intended for debugging or manual review."
        )]
        string format = "toon")
    {
        return query.GetTypeSchema(id, ParseFormat(format)).Schema;
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

    private static OutputFormat ParseFormat(string format) =>
        format == "json" ? OutputFormat.Json : OutputFormat.Toon;

    private static string SerializeUsage<T>(T value, string format)
    {
        var json = JsonSerializer.SerializeToNode(value, ProtodefType.DefaultJsonOptions);
        if (format == "toon") return ToonSerializer.Encode(json);
        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
