#:package Microsoft.Extensions.Hosting@10.0.5
#:package ModelContextProtocol@1.1.0
#:project ../src/PacketGenerator.Protocol/PacketGenerator.Protocol.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property WarningLevel=0
#:property NoWarn=NU1510;NU1901;NU1902;NU1903;NU1904;CS0436;CS0659;CS0660;CS0661;CS2002;CS8601;CS8602;CS8604;CS8618;CS8619;CS8620;IL2026;IL3050

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PacketGenerator.Protocol.Graph;
using PacketGenerator.Protocol.Loading;
using PacketGenerator.Protocol.Queries;
using PacketGenerator.Protocol.Repository;
using PacketGenerator.Protocol.Serialization;

var repository = await ProtocolDataLoader.LoadRepositoryAsync(new ProtocolDataOptions());

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton<IProtocolRepository>(repository);
builder.Services.AddSingleton(new ProtocolQueryService(repository));
builder.Services.AddSingleton(new ProtocolUsageQueries(repository));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class PacketGeneratorMcpTools
{
    [McpServerTool(Name = "list_packets")]
    [Description("Lists packet ids grouped by namespace. Optional filter is case-insensitive and may contain tokens separated by '|'.")]
    public static string ListPackets(ProtocolQueryService query, string? filter = null, string format = "json") =>
        Serialize(query.GetPackets(filter), format);

    [McpServerTool(Name = "list_types")]
    [Description("Lists protocol type ids. Optional filter is case-insensitive and may contain tokens separated by '|'.")]
    public static string ListTypes(ProtocolQueryService query, string? filter = null, string format = "json") =>
        Serialize(query.GetTypes(filter), format);

    [McpServerTool(Name = "list_native_types")]
    [Description("Lists native/protodef primitive type names.")]
    public static string ListNativeTypes(ProtocolQueryService query, string format = "json") =>
        Serialize(query.GetNativeTypes(), format);

    [McpServerTool(Name = "list_types_by_kind")]
    [Description("Groups protocol type ids by protodef kind.")]
    public static string ListTypesByKind(ProtocolQueryService query, string format = "json") =>
        Serialize(query.GetTypesByKind(), format);

    [McpServerTool(Name = "get_packet_schema")]
    [Description("Returns the versioned schema for a packet id, for example play.toClient.keep_alive.")]
    public static string GetPacketSchema(ProtocolQueryService query, string id, string format = "toon") =>
        Serialize(query.GetPacketSchema(id, ParseFormat(format)), format);

    [McpServerTool(Name = "get_type_schema")]
    [Description("Returns the versioned schema for a protocol type id.")]
    public static string GetTypeSchema(ProtocolQueryService query, string id, string format = "toon") =>
        Serialize(query.GetTypeSchema(id, ParseFormat(format)), format);

    [McpServerTool(Name = "get_packet_composition")]
    [Description("Returns the set of protodef/native type kinds used by a packet.")]
    public static string GetPacketComposition(ProtocolQueryService query, string id, string format = "json") =>
        Serialize(query.GetPacketComposition(id), format);

    [McpServerTool(Name = "get_protocol_usage")]
    [Description("Returns compact usage statistics for packet/type/native/shape targets. Use top to limit output and kind to filter: packet, type, native, or shape.")]
    public static string GetProtocolUsage(ProtocolUsageQueries usage, int? top = 25, string? kind = null, string format = "json") =>
        Serialize(usage.GetUsage(top, kind), format);

    [McpServerTool(Name = "get_protocol_users")]
    [Description("Returns where a packet, type, native type, or shape is used. Accepts ids like play.toServer.window_click, HashedSlot, type:HashedSlot, native:varint, or shape:container.")]
    public static string GetProtocolUsers(ProtocolUsageQueries usage, string id, string format = "toon") =>
        Serialize(usage.GetUsers(id), format);

    [McpServerTool(Name = "get_protocol_dependencies")]
    [Description("Returns compact dependencies used by a packet or protocol type, including target path, version ranges, and field paths.")]
    public static string GetProtocolDependencies(ProtocolUsageQueries usage, string id, string format = "toon") =>
        Serialize(usage.GetDependencies(id), format);

    [McpServerTool(Name = "get_protocol_stats")]
    [Description("Returns packet counts and structural complexity tier stats.")]
    public static string GetProtocolStats(ProtocolQueryService query, string format = "json") =>
        Serialize(query.GetStats(), format);

    [McpServerTool(Name = "get_protocol_graph")]
    [Description("Returns protocol graph nodes/edges for packets, named types, native types, and protodef shapes.")]
    public static string GetProtocolGraph(
        IProtocolRepository repository,
        string? ns = "play",
        string? direction = "toClient",
        bool includeTypes = true,
        string format = "json") =>
        Serialize(new ProtocolGraphBuilder(repository).Build(ns, direction, includeTypes), format);

    private static string Serialize<T>(T value, string format)
    {
        var parsed = ParseFormat(format);
        var json = JsonSerializer.SerializeToNode(value, JsonOptions())!;
        return ProtocolSchemaSerializer.Serialize(json, parsed);
    }

    private static OutputFormat ParseFormat(string format)
    {
        if (OutputFormatParser.TryParse(format, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid format '{format}'. Valid values: json, toon.");
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        foreach (var converter in Protodef.ProtodefType.DefaultJsonOptions.Converters)
            options.Converters.Add(converter);
        return options;
    }
}
