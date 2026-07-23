using System;
using System.Collections.Generic;
using System.Linq;
using McProtoFacts.Protocol.Complexity;
using McProtoFacts.Protocol.Repository;
using McProtoFacts.Protocol.Serialization;
using Protodef;

namespace McProtoFacts.Protocol.Queries;

public sealed class ProtocolQueryService
{
    private readonly IProtocolRepository _repository;
    private readonly ComplexityThresholds _thresholds;

    public ProtocolQueryService(
        IProtocolRepository repository,
        ComplexityThresholds? thresholds = null)
    {
        _repository = repository;
        _thresholds = thresholds ?? new ComplexityThresholds();
    }

    public IReadOnlyDictionary<string, string[]> GetPackets(string? filter = null, string? tier = null)
    {
        var packets = _repository.GetPackets()
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(p => MatchesFilter($"{kv.Key}.{p.Key}", filter) && MatchesTier(p.Value.History, tier))
                    .Select(p => p.Key)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray());

        return packets
            .Where(kv => kv.Value.Length > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private bool MatchesTier(Dictionary<ProtocolRange, ProtodefType?> history, string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) return true;
        var score = PacketComplexityScorer.Compute(history);
        return _thresholds.Classify(score).ToLabel() == tier;
    }

    public string[] GetTypes(string? filter = null)
    {
        return _repository.GetTypes()
            .Where(id => MatchesFilter(id, filter))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public string[] GetNativeTypes() =>
        _repository.GetNativeTypes()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyDictionary<string, string[]> GetTypesByKind()
    {
        var grouped = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var typeId in _repository.GetTypes())
        {
            var kind = "unknown";
            try
            {
                var typeHistory = _repository.GetTypeHistory(typeId);
                foreach (var (_, protodefType) in typeHistory.History)
                {
                    if (protodefType is null) continue;
                    kind = ProtodefTypeAnalyzer.GetKindName(protodefType);
                    break;
                }
            }
            catch
            {
                kind = "error";
            }

            if (!grouped.TryGetValue(kind, out var values))
                grouped[kind] = values = new List<string>();
            values.Add(typeId);
        }

        return grouped.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    public SchemaResult GetPacketSchema(string id, OutputFormat format)
    {
        var packet = _repository.GetPacket(id);
        var supported = _repository.GetSupportedProtocols();
        var json = ProtocolSchemaSerializer.ToJsonNode(packet.History, supported);
        var score = PacketComplexityScorer.Compute(packet.History);

        return new SchemaResult(
            Id: id,
            Name: packet.Name,
            Format: format,
            Schema: ProtocolSchemaSerializer.Serialize(json, format),
            Json: ProtocolSchemaSerializer.Serialize(json, OutputFormat.Json),
            Toon: ProtocolSchemaSerializer.Serialize(json, OutputFormat.Toon),
            ComplexityScore: score,
            Tier: _thresholds.Classify(score).ToLabel());
    }

    public SchemaResult GetTypeSchema(string id, OutputFormat format)
    {
        var type = _repository.GetTypeHistory(id);
        var supported = _repository.GetSupportedProtocols();
        var json = ProtocolSchemaSerializer.ToJsonNode(type.History, supported);
        var score = PacketComplexityScorer.Compute(type.History);

        return new SchemaResult(
            Id: id,
            Name: type.Name,
            Format: format,
            Schema: ProtocolSchemaSerializer.Serialize(json, format),
            Json: ProtocolSchemaSerializer.Serialize(json, OutputFormat.Json),
            Toon: ProtocolSchemaSerializer.Serialize(json, OutputFormat.Toon),
            ComplexityScore: score,
            Tier: _thresholds.Classify(score).ToLabel());
    }

    public string[] GetPacketComposition(string id)
    {
        var packet = _repository.GetPacket(id);
        var composition = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, type) in packet.History)
        {
            if (type is null) continue;
            composition.UnionWith(ProtodefTypeAnalyzer.GetTypeComposition(type.CreatePrimitiveResolvedCopy()));
        }

        return composition.OrderBy(kind => kind, StringComparer.Ordinal).ToArray();
    }

    public ProtocolStats GetStats()
    {
        int total = 0, tiny = 0, easy = 0, medium = 0, heavy = 0;
        var byNamespace = new SortedDictionary<string, ProtocolNamespaceStats>(StringComparer.Ordinal);
        var packets = new List<ProtocolPacketStats>();

        foreach (var (ns, packetMap) in _repository.GetPackets())
        {
            int nsTotal = 0, nsTiny = 0, nsEasy = 0, nsMedium = 0, nsHeavy = 0;
            foreach (var (name, def) in packetMap)
            {
                var score = PacketComplexityScorer.Compute(def.History);
                var tier = _thresholds.Classify(score);
                var tierLabel = tier.ToLabel();
                total++;
                nsTotal++;
                switch (tier)
                {
                    case ComplexityTier.Tiny:
                        tiny++;
                        nsTiny++;
                        break;
                    case ComplexityTier.Easy:
                        easy++;
                        nsEasy++;
                        break;
                    case ComplexityTier.Medium:
                        medium++;
                        nsMedium++;
                        break;
                    default:
                        heavy++;
                        nsHeavy++;
                        break;
                }

                packets.Add(new ProtocolPacketStats($"{ns}.{name}", score, tierLabel));
            }

            byNamespace[ns] = new ProtocolNamespaceStats(ns, nsTotal, nsTiny, nsEasy, nsMedium, nsHeavy);
        }

        return new ProtocolStats(
            total,
            new ProtocolTierStats(tiny, easy, medium, heavy),
            byNamespace.Values.ToArray(),
            packets.OrderBy(packet => packet.Id, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Orders every named type from simplest to most complex by a topological layering of the
    /// type-to-type dependency graph. Strongly-connected components (genuinely recursive/mutually
    /// dependent types such as Slot &lt;-&gt; SlotComponent) are condensed into a single group so the
    /// graph is layerable; each type's layer = 1 + max(layer of its dependencies). Within a layer,
    /// types are ordered by structural complexity score. Layer 0 depends only on native/shape kinds.
    /// </summary>
    public BuildOrderResult GetBuildOrder()
    {
        var edges = new ProtocolUsageQueries(_repository).GetTypeDependencies();
        var names = edges.Select(edge => edge.Name).ToArray();
        var deps = edges.ToDictionary(
            edge => edge.Name,
            edge => (IReadOnlyList<string>)edge.Deps,
            StringComparer.Ordinal);
        var selfRecursive = edges.ToDictionary(edge => edge.Name, edge => edge.SelfRecursive, StringComparer.Ordinal);

        var score = new Dictionary<string, int>(StringComparer.Ordinal);
        var tier = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var s = PacketComplexityScorer.Compute(_repository.GetTypeHistory(name).History);
            score[name] = s;
            tier[name] = _thresholds.Classify(s).ToLabel();
        }

        // Condense strongly-connected components so the cyclic core becomes one node in a DAG.
        var sccs = TarjanScc(names, deps);
        var sccOf = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < sccs.Count; i++)
            foreach (var name in sccs[i])
                sccOf[name] = i;

        var compDeps = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < sccs.Count; i++) compDeps[i] = new HashSet<int>();
        foreach (var name in names)
            foreach (var dep in deps[name])
                if (sccOf[dep] != sccOf[name])
                    compDeps[sccOf[name]].Add(sccOf[dep]);

        // Longest-path layering over the (acyclic) condensation.
        var compLayer = new Dictionary<int, int>();
        int LayerOf(int comp)
        {
            if (compLayer.TryGetValue(comp, out var cached)) return cached;
            var max = -1;
            foreach (var dep in compDeps[comp]) max = Math.Max(max, LayerOf(dep));
            return compLayer[comp] = max + 1;
        }
        for (var i = 0; i < sccs.Count; i++) LayerOf(i);

        var sccMinScore = new Dictionary<int, int>();
        for (var i = 0; i < sccs.Count; i++) sccMinScore[i] = sccs[i].Min(name => score[name]);

        // Keep members of a recursive group contiguous: order by (layer, group min score, group, score, name).
        var ordered = names
            .OrderBy(name => compLayer[sccOf[name]])
            .ThenBy(name => sccMinScore[sccOf[name]])
            .ThenBy(name => sccOf[name])
            .ThenBy(name => score[name])
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Renumber SCC ids into stable, sequential group ids following output order.
        var groupRemap = new Dictionary<int, int>();
        var nextGroup = 0;
        foreach (var name in ordered)
            if (!groupRemap.ContainsKey(sccOf[name]))
                groupRemap[sccOf[name]] = nextGroup++;

        var entries = ordered
            .Select(name => new BuildOrderEntry(
                Name: name,
                Layer: compLayer[sccOf[name]],
                Group: groupRemap[sccOf[name]],
                Recursive: sccs[sccOf[name]].Count > 1 || selfRecursive[name],
                Score: score[name],
                Tier: tier[name],
                Deps: deps[name]))
            .ToArray();

        var layerCount = entries.Length == 0 ? 0 : entries.Max(entry => entry.Layer) + 1;
        return new BuildOrderResult(entries.Length, layerCount, entries);
    }

    /// <summary>Tarjan's strongly-connected-components over a bare-name adjacency map.</summary>
    private static List<List<string>> TarjanScc(
        IReadOnlyList<string> nodes,
        IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        var low = new Dictionary<string, int>(StringComparer.Ordinal);
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var components = new List<List<string>>();
        var counter = 0;

        void StrongConnect(string v)
        {
            index[v] = counter;
            low[v] = counter;
            counter++;
            stack.Push(v);
            onStack.Add(v);

            foreach (var w in adjacency[v])
            {
                if (!index.ContainsKey(w))
                {
                    StrongConnect(w);
                    low[v] = Math.Min(low[v], low[w]);
                }
                else if (onStack.Contains(w))
                {
                    low[v] = Math.Min(low[v], index[w]);
                }
            }

            if (low[v] != index[v]) return;

            var component = new List<string>();
            string popped;
            do
            {
                popped = stack.Pop();
                onStack.Remove(popped);
                component.Add(popped);
            } while (!string.Equals(popped, v, StringComparison.Ordinal));
            components.Add(component);
        }

        foreach (var node in nodes)
            if (!index.ContainsKey(node))
                StrongConnect(node);

        return components;
    }

    private static bool MatchesFilter(string value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record SchemaResult(
    string Id,
    string Name,
    OutputFormat Format,
    string Schema,
    string Json,
    string Toon,
    int ComplexityScore,
    string Tier);

public sealed record ProtocolTierStats(int Tiny, int Easy, int Medium, int Heavy);

public sealed record ProtocolNamespaceStats(
    string Ns,
    int Total,
    int Tiny,
    int Easy,
    int Medium,
    int Heavy);

public sealed record ProtocolPacketStats(string Id, int Score, string Tier);

public sealed record ProtocolStats(
    int Total,
    ProtocolTierStats Tiers,
    IReadOnlyCollection<ProtocolNamespaceStats> ByNamespace,
    IReadOnlyCollection<ProtocolPacketStats> Packets);

public sealed record BuildOrderResult(
    int TypeCount,
    int LayerCount,
    IReadOnlyList<BuildOrderEntry> Types);

public sealed record BuildOrderEntry(
    string Name,
    int Layer,
    int Group,
    bool Recursive,
    int Score,
    string Tier,
    IReadOnlyList<string> Deps);
