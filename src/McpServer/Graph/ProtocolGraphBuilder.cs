using System;
using System.Collections.Generic;
using System.Linq;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using ProtoCore;
using Protodef;
using Protodef.Enumerable;

namespace McpServer.Graph;

public sealed class ProtocolGraphBuilder
{
    private static readonly HashSet<string> ShapeKinds = new(StringComparer.Ordinal)
    {
        "container",
        "array",
        "buffer",
        "switch",
        "cus_switch",
        "mapper",
        "bitfield",
        "bitflags",
        "option",
        "loop",
        "topBitSetTerminatedArray",
        "registryEntryHolder",
        "registryEntryHolderSet"
    };

    private static readonly HashSet<string> PrimitiveKinds = new(StringComparer.Ordinal)
    {
        "varint", "varlong", "bool", "void", "string", "pstring",
        "i8", "u8", "i16", "u16", "i32", "u32", "i64", "u64", "f32", "f64",
        "li8", "lu8", "li16", "lu16", "li32", "lu32", "li64", "lu64", "lf32", "lf64"
    };

    private readonly IProtocolRepository _repository;
    private readonly ModelConfigService _modelConfig;
    private readonly HashSet<string> _nativeTypes;
    private readonly HashSet<string> _knownTypes;
    private readonly Dictionary<string, string> _knownTypeAliases;
    private readonly Dictionary<string, ProtocolGraphNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ProtocolGraphEdge> _edges = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _namedTypePacketRefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _shapeRefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _packetsByTier = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _nodeRanges = new(StringComparer.Ordinal);

    public ProtocolGraphBuilder(IProtocolRepository repository, ModelConfigService modelConfig)
    {
        _repository = repository;
        _modelConfig = modelConfig;
        _nativeTypes = repository.GetNativeTypes().ToHashSet(StringComparer.Ordinal);
        _knownTypes = repository.GetTypes().ToHashSet(StringComparer.Ordinal);
        _knownTypeAliases = _knownTypes
            .GroupBy(ShortTypeName, StringComparer.Ordinal)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal);
    }

    public ProtocolGraph Build(string? ns = null, string? direction = null, bool includeTypeDefinitions = true)
    {
        foreach (var (packetNamespace, packets) in _repository.GetPackets().OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!MatchesNamespace(packetNamespace, ns, direction)) continue;

            foreach (var (packetName, packet) in packets.OrderBy(x => x.Key, StringComparer.Ordinal))
                AddPacket(packetNamespace, packetName, packet);
        }

        if (includeTypeDefinitions)
        {
            foreach (var typeId in _repository.GetTypes().OrderBy(x => x, StringComparer.Ordinal))
                AddTypeDefinition(typeId);
        }

        var nodes = _nodes.Values
            .Select(n => n with
            {
                ReuseCount = n.Kind == "namedType" && _namedTypePacketRefs.TryGetValue(n.Id, out var refs) ? refs.Count : n.ReuseCount,
                VersionRanges = _nodeRanges.TryGetValue(n.Id, out var ranges) ? ranges.OrderBy(x => x, StringComparer.Ordinal).ToArray() : n.VersionRanges
            })
            .OrderBy(n => KindOrder(n.Kind))
            .ThenByDescending(n => n.ReuseCount)
            .ThenBy(n => n.Id, StringComparer.Ordinal)
            .ToArray();

        var edges = _edges.Values
            .OrderBy(e => e.From, StringComparer.Ordinal)
            .ThenBy(e => e.To, StringComparer.Ordinal)
            .ThenBy(e => e.FieldPath, StringComparer.Ordinal)
            .ThenBy(e => e.VersionRange, StringComparer.Ordinal)
            .ToArray();

        var stats = new ProtocolGraphStats(
            PacketCount: nodes.Count(n => n.Kind == "packet"),
            NamedTypeCount: nodes.Count(n => n.Kind == "namedType"),
            NativeTypeCount: nodes.Count(n => n.Kind == "nativeType"),
            ShapeCount: nodes.Count(n => n.Kind == "shape"),
            EdgeCount: edges.Length,
            TopNamedTypes: _namedTypePacketRefs
                .Select(kv => new ProtocolGraphRank(kv.Key, LabelFromNodeId(kv.Key), kv.Value.Count))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label, StringComparer.Ordinal)
                .Take(25)
                .ToArray(),
            TopShapes: _shapeRefs
                .Select(kv => new ProtocolGraphRank(ShapeId(kv.Key), kv.Key, kv.Value))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label, StringComparer.Ordinal)
                .Take(25)
                .ToArray(),
            PacketsByTier: new SortedDictionary<string, int>(_packetsByTier, StringComparer.Ordinal));

        return new ProtocolGraph(nodes, edges, stats);
    }

    private void AddPacket(string packetNamespace, string packetName, PacketDefinition packet)
    {
        var id = PacketId(packetNamespace, packetName);
        var score = PacketComplexityScorer.Compute(packet.History);
        var tier = _modelConfig.ClassifyTier(score).ToLabel();
        Increment(_packetsByTier, tier);

        var parts = packetNamespace.Split('.', 2);
        AddNode(new ProtocolGraphNode(
            Id: id,
            Label: packetName,
            Kind: "packet",
            ComplexityScore: score,
            Tier: tier,
            Namespace: parts.ElementAtOrDefault(0),
            Direction: parts.ElementAtOrDefault(1),
            VersionRanges: packet.History.Keys.Select(RangeLabel).ToArray()));

        foreach (var (range, type) in packet.History)
        {
            AddRange(id, RangeLabel(range));
            if (type is null) continue;
            ScanTree(id, type.CreatePrimitiveResolvedCopy(), packetSourceId: id, range: RangeLabel(range), path: "");
        }
    }

    private void AddTypeDefinition(string typeId)
    {
        TypeHistory history;
        try { history = _repository.GetTypeHistory(typeId); }
        catch { return; }

        var score = PacketComplexityScorer.Compute(history.History);
        var tier = _modelConfig.ClassifyTier(score).ToLabel();
        var nodeId = NamedTypeId(typeId);
        AddNode(new ProtocolGraphNode(
            Id: nodeId,
            Label: typeId,
            Kind: "namedType",
            ComplexityScore: score,
            Tier: tier,
            Known: true,
            VersionRanges: history.History.Keys.Select(RangeLabel).ToArray()));

        foreach (var (range, type) in history.History)
        {
            AddRange(nodeId, RangeLabel(range));
            if (type is null) continue;
            ScanTree(nodeId, type.CreatePrimitiveResolvedCopy(), packetSourceId: null, range: RangeLabel(range), path: "");
        }
    }

    private void ScanTree(string ownerId, ProtodefType type, string? packetSourceId, string range, string path)
    {
        var kindName = ProtodefTypeAnalyzer.GetKindName(type);
        var targetId = AddTypeNode(kindName);

        if (targetId is not null)
        {
            AddRange(targetId, range);
            AddEdge(ownerId, targetId, type, path, range);

            if (packetSourceId is not null && IsNamedTypeId(targetId))
            {
                if (!_namedTypePacketRefs.TryGetValue(targetId, out var refs))
                    _namedTypePacketRefs[targetId] = refs = new HashSet<string>(StringComparer.Ordinal);
                refs.Add(packetSourceId);
            }
        }

        if (ShapeKinds.Contains(kindName))
            Increment(_shapeRefs, kindName);

        foreach (var (childKey, child) in type.Children)
        {
            var childPath = string.IsNullOrEmpty(path)
                ? childKey ?? "_"
                : path + "." + (childKey ?? "_");
            ScanTree(ownerId, child, packetSourceId, range, childPath);
        }
    }

    private string? AddTypeNode(string kindName)
    {
        if (ShapeKinds.Contains(kindName))
        {
            var id = ShapeId(kindName);
            AddNode(new ProtocolGraphNode(id, kindName, "shape"));
            return id;
        }

        if (PrimitiveKinds.Contains(kindName) || _nativeTypes.Contains(kindName))
        {
            var id = NativeId(kindName);
            AddNode(new ProtocolGraphNode(id, kindName, "nativeType"));
            return id;
        }

        var resolvedName = ResolveKnownTypeName(kindName);
        var known = _knownTypes.Contains(resolvedName);
        var nodeId = NamedTypeId(resolvedName);
        AddNode(new ProtocolGraphNode(nodeId, ShortTypeName(resolvedName), "namedType", Known: known));
        return nodeId;
    }

    private void AddEdge(string from, string to, ProtodefType type, string path, string range)
    {
        var edgeKind = to.StartsWith("shape:", StringComparison.Ordinal) ? "containsShape" : "usesType";
        var caseLabel = TryGetSwitchCase(path);
        var id = $"{from}->{to}|{edgeKind}|{path}|{range}|{caseLabel}";
        if (_edges.ContainsKey(id)) return;
        _edges[id] = new ProtocolGraphEdge(id, from, to, edgeKind, EmptyToNull(path), range, caseLabel);
    }

    private void AddNode(ProtocolGraphNode node)
    {
        if (!_nodes.TryGetValue(node.Id, out var existing))
        {
            _nodes[node.Id] = node;
            return;
        }

        _nodes[node.Id] = existing with
        {
            ComplexityScore = existing.ComplexityScore ?? node.ComplexityScore,
            Tier = existing.Tier ?? node.Tier,
            Namespace = existing.Namespace ?? node.Namespace,
            Direction = existing.Direction ?? node.Direction,
            Known = existing.Known ?? node.Known
        };
    }

    private void AddRange(string nodeId, string range)
    {
        if (!_nodeRanges.TryGetValue(nodeId, out var ranges))
            _nodeRanges[nodeId] = ranges = new HashSet<string>(StringComparer.Ordinal);
        ranges.Add(range);
    }

    private static bool MatchesNamespace(string packetNamespace, string? ns, string? direction)
    {
        if (!string.IsNullOrWhiteSpace(ns) && !packetNamespace.StartsWith(ns + ".", StringComparison.Ordinal)) return false;
        if (!string.IsNullOrWhiteSpace(direction) && !packetNamespace.EndsWith("." + direction, StringComparison.Ordinal)) return false;
        return true;
    }

    private static string? TryGetSwitchCase(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var last = path.Split('.').LastOrDefault();
        return last is "type" or "countType" or "default" or null ? null : last;
    }

    private static void Increment(Dictionary<string, int> dictionary, string key)
    {
        dictionary[key] = dictionary.TryGetValue(key, out var value) ? value + 1 : 1;
    }

    private string ResolveKnownTypeName(string typeName)
    {
        if (_knownTypes.Contains(typeName)) return typeName;
        return _knownTypeAliases.TryGetValue(typeName, out var resolved) ? resolved : typeName;
    }

    private static string ShortTypeName(string typeName)
    {
        var index = typeName.LastIndexOf('.');
        return index >= 0 ? typeName[(index + 1)..] : typeName;
    }

    private static string PacketId(string packetNamespace, string packetName) => $"packet:{packetNamespace}.{packetName}";
    private static string NamedTypeId(string typeName) => $"type:{typeName}";
    private static string NativeId(string typeName) => $"native:{typeName}";
    private static string ShapeId(string kindName) => $"shape:{kindName}";
    private static bool IsNamedTypeId(string id) => id.StartsWith("type:", StringComparison.Ordinal);
    private static string LabelFromNodeId(string id) => id.Contains(':') ? id[(id.IndexOf(':') + 1)..] : id;
    private static string? EmptyToNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    private static int KindOrder(string kind) => kind switch { "packet" => 0, "namedType" => 1, "shape" => 2, "nativeType" => 3, _ => 9 };
    private static string RangeLabel(ProtocolRange range) => range.From == range.To ? range.From.ToString() : $"{range.From}-{range.To}";
}
