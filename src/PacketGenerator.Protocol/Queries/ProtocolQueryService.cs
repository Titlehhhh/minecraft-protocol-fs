using System;
using System.Collections.Generic;
using System.Linq;
using PacketGenerator.Protocol.Complexity;
using PacketGenerator.Protocol.Repository;
using PacketGenerator.Protocol.Serialization;
using Protodef;

namespace PacketGenerator.Protocol.Queries;

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

    public IReadOnlyDictionary<string, string[]> GetPackets(string? filter = null)
    {
        var packets = _repository.GetPackets()
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Keys
                    .Where(name => MatchesFilter($"{kv.Key}.{name}", filter))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray());

        return packets
            .Where(kv => kv.Value.Length > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
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
