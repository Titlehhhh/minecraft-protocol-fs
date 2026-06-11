using System.Collections.Generic;

namespace McpServer.Graph;

public sealed record ProtocolGraph(
    IReadOnlyCollection<ProtocolGraphNode> Nodes,
    IReadOnlyCollection<ProtocolGraphEdge> Edges,
    ProtocolGraphStats Stats);

public sealed record ProtocolGraphNode(
    string Id,
    string Label,
    string Kind,
    int? ComplexityScore = null,
    string? Tier = null,
    string? Namespace = null,
    string? Direction = null,
    bool? Known = null,
    int ReuseCount = 0,
    IReadOnlyCollection<string>? VersionRanges = null);

public sealed record ProtocolGraphEdge(
    string Id,
    string From,
    string To,
    string Kind,
    string? FieldPath = null,
    string? VersionRange = null,
    string? Case = null);

public sealed record ProtocolGraphStats(
    int PacketCount,
    int NamedTypeCount,
    int NativeTypeCount,
    int ShapeCount,
    int EdgeCount,
    IReadOnlyCollection<ProtocolGraphRank> TopNamedTypes,
    IReadOnlyCollection<ProtocolGraphRank> TopShapes,
    IReadOnlyDictionary<string, int> PacketsByTier);

public sealed record ProtocolGraphRank(string Id, string Label, int Count);
