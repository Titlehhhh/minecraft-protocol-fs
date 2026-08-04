using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using McProtoFacts.Protocol.Queries;
using McProtoFacts.Protocol.Serialization;
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

    [McpServerTool(UseStructuredContent = false)]
    [Description(
        "Hybrid search (BM25 + optional semantic embeddings) over all protocol packets and named types across all versions. " +
        "RECOMMENDED FIRST STEP for any broad task: find relevant packets/types by concept before drilling down with get_packet/get_type. " +
        "Query can mix identifiers and natural language, e.g. 'chunk sections payload', 'team color formatting', 'varint length prefix'. " +
        "Returns matching owners (packets/types) ranked by relevance with their most relevant chunk paths and version ranges."
    )]
    public static async Task<string> SearchProtocol(
        McpServer.Services.HybridSearchService search,
        string query,
        [Description("Maximum number of packets/types to return (1-50, default 8).")]
        int limit = 8,
        CancellationToken ct = default)
    {
        var response = await search.SearchAsync(query, limit, ct);
        if (response.Owners.Count == 0) return $"mode: {response.Mode}\nno matches";

        var sb = new StringBuilder();
        sb.Append("mode: ").Append(response.Mode).Append('\n');
        var rank = 0;
        foreach (var owner in response.Owners)
        {
            rank++;
            sb.Append(rank).Append(". ").Append(owner.Owner)
              .Append("  score=").Append(owner.Score.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n');
            foreach (var chunk in owner.Chunks)
            {
                sb.Append("   - ").Append(chunk.ChunkKind)
                  .Append(' ').Append(chunk.Path)
                  .Append(" [").Append(chunk.VersionRange).Append(']').Append('\n');
            }
        }
        sb.Append("Use get_packet / get_type with an owner id for the full versioned schema.");
        return sb.ToString();
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
