using System;
using System.Collections.Generic;
using System.Linq;
using PacketGenerator.Protocol.Repository;
using Protodef;

namespace PacketGenerator.Protocol.Queries;

public sealed class ProtocolUsageQueries
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
    private readonly HashSet<string> _nativeTypes;
    private readonly HashSet<string> _knownTypes;
    private readonly Dictionary<string, string> _knownTypeAliases;

    public ProtocolUsageQueries(IProtocolRepository repository)
    {
        _repository = repository;
        _nativeTypes = repository.GetNativeTypes().ToHashSet(StringComparer.Ordinal);
        _knownTypes = repository.GetTypes().ToHashSet(StringComparer.Ordinal);
        _knownTypeAliases = _knownTypes
            .GroupBy(ShortTypeName, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
    }

    public UsageSummary[] GetUsage(int? top = null, string? targetKind = null)
    {
        IEnumerable<UsageSummary> summaries = GetUsageRecords()
            .Where(record => MatchesKind(record.TargetKind, targetKind))
            .GroupBy(record => record.TargetId, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new UsageSummary(
                    TargetId: first.TargetId,
                    TargetKind: first.TargetKind,
                    Label: first.TargetLabel,
                    Path: first.TargetPath,
                    UsageCount: group.Count(),
                    OwnerCount: group.Select(record => record.OwnerId).Distinct(StringComparer.Ordinal).Count(),
                    VersionRanges: group.Select(record => record.VersionRange).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            })
            .OrderByDescending(summary => summary.UsageCount)
            .ThenBy(summary => summary.TargetKind, StringComparer.Ordinal)
            .ThenBy(summary => summary.TargetId, StringComparer.Ordinal);

        if (top is > 0)
            summaries = summaries.Take(top.Value);

        return summaries.ToArray();
    }

    public UsageRecord[] GetUsers(string id)
    {
        var normalized = NormalizeLookupId(id);
        return GetUsageRecords()
            .Where(record => string.Equals(record.TargetId, normalized, StringComparison.Ordinal))
            .OrderBy(record => record.OwnerKind, StringComparer.Ordinal)
            .ThenBy(record => record.OwnerId, StringComparer.Ordinal)
            .ThenBy(record => record.VersionRange, StringComparer.Ordinal)
            .ThenBy(record => record.FieldPath, StringComparer.Ordinal)
            .ToArray();
    }

    public DependencyResult GetDependencies(string id)
    {
        var normalized = NormalizeOwnerId(id);
        var owner = GetOwners().FirstOrDefault(candidate => string.Equals(candidate.Id, normalized, StringComparison.Ordinal));
        if (owner is null)
            throw new KeyNotFoundException($"No packet or type found with id {id}");

        var dependencies = GetUsageRecords()
            .Where(record => string.Equals(record.OwnerId, owner.Id, StringComparison.Ordinal))
            .GroupBy(record => record.TargetId, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new DependencySummary(
                    TargetId: first.TargetId,
                    TargetKind: first.TargetKind,
                    Label: first.TargetLabel,
                    Path: first.TargetPath,
                    UsageCount: group.Count(),
                    VersionRanges: group.Select(record => record.VersionRange).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    FieldPaths: group.Select(record => record.FieldPath).Where(path => path is not null).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray()!);
            })
            .OrderBy(summary => KindOrder(summary.TargetKind))
            .ThenBy(summary => summary.TargetId, StringComparer.Ordinal)
            .ToArray();

        return new DependencyResult(owner.Id, owner.Kind, owner.Path, dependencies);
    }

    private UsageRecord[] GetUsageRecords()
    {
        var records = new List<UsageRecord>();

        foreach (var owner in GetOwners())
        {
            foreach (var (range, type) in owner.History)
            {
                if (type is null) continue;

                var sourcePath = string.IsNullOrWhiteSpace(type.Path) ? owner.Path : type.Path;

                if (owner.Kind == "packet")
                {
                    var packetNamespace = PacketNamespaceFromId(owner.Id);
                    var packetName = PacketNameFromId(owner.Id);
                    records.Add(new UsageRecord(
                        OwnerId: RegistryId(packetNamespace),
                        OwnerKind: "registry",
                        OwnerPath: packetNamespace,
                        SourcePath: packetNamespace,
                        TargetId: owner.Id,
                        TargetKind: "packet",
                        TargetPath: sourcePath,
                        TargetLabel: packetName,
                        VersionRange: RangeLabel(range),
                        FieldPath: packetName,
                        Case: null));
                }

                Scan(owner, type.CreatePrimitiveResolvedCopy(), RangeLabel(range), sourcePath, "", records);
            }
        }

        return records
            .OrderBy(record => record.OwnerId, StringComparer.Ordinal)
            .ThenBy(record => record.TargetId, StringComparer.Ordinal)
            .ThenBy(record => record.VersionRange, StringComparer.Ordinal)
            .ThenBy(record => record.FieldPath, StringComparer.Ordinal)
            .ToArray();
    }

    private IEnumerable<UsageOwner> GetOwners()
    {
        foreach (var (packetNamespace, packets) in _repository.GetPackets().OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            foreach (var (packetName, packet) in packets.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                var id = PacketId(packetNamespace, packetName);
                var path = FirstPath(packet.History) ?? id;
                yield return new UsageOwner(id, "packet", path, packet.History);
            }
        }

        foreach (var typeId in _repository.GetTypes().OrderBy(id => id, StringComparer.Ordinal))
        {
            var type = _repository.GetTypeHistory(typeId);
            var path = FirstPath(type.History) ?? type.Id;
            yield return new UsageOwner(TypeId(typeId), "type", path, type.History);
        }
    }

    private void Scan(
        UsageOwner owner,
        ProtodefType type,
        string range,
        string sourcePath,
        string fieldPath,
        List<UsageRecord> records)
    {
        var kindName = ProtodefTypeAnalyzer.GetKindName(type);
        var target = ClassifyTarget(kindName);

        records.Add(new UsageRecord(
            OwnerId: owner.Id,
            OwnerKind: owner.Kind,
            OwnerPath: owner.Path,
            SourcePath: sourcePath,
            TargetId: target.Id,
            TargetKind: target.Kind,
            TargetPath: target.Path,
            TargetLabel: target.Label,
            VersionRange: range,
            FieldPath: EmptyToNull(fieldPath),
            Case: TryGetSwitchCase(fieldPath)));

        foreach (var (childKey, child) in type.Children)
        {
            var childPath = string.IsNullOrEmpty(fieldPath)
                ? childKey ?? "_"
                : fieldPath + "." + (childKey ?? "_");
            Scan(owner, child, range, sourcePath, childPath, records);
        }
    }

    private UsageTarget ClassifyTarget(string kindName)
    {
        if (ShapeKinds.Contains(kindName))
            return new UsageTarget(ShapeId(kindName), "shape", kindName, kindName);

        if (PrimitiveKinds.Contains(kindName) || _nativeTypes.Contains(kindName))
            return new UsageTarget(NativeId(kindName), "native", kindName, kindName);

        var resolvedName = ResolveKnownTypeName(kindName);
        return new UsageTarget(TypeId(resolvedName), "type", resolvedName, ShortTypeName(resolvedName));
    }

    private string NormalizeLookupId(string id)
    {
        if (id.StartsWith("packet:", StringComparison.Ordinal) ||
            id.StartsWith("type:", StringComparison.Ordinal) ||
            id.StartsWith("native:", StringComparison.Ordinal) ||
            id.StartsWith("shape:", StringComparison.Ordinal))
            return id;

        if (ShapeKinds.Contains(id))
            return ShapeId(id);

        if (PrimitiveKinds.Contains(id) || _nativeTypes.Contains(id))
            return NativeId(id);

        if (LooksLikePacketId(id) && _repository.ContainsPacket(id))
            return PacketId(id);

        return TypeId(ResolveKnownTypeName(id));
    }

    private string NormalizeOwnerId(string id)
    {
        if (id.StartsWith("packet:", StringComparison.Ordinal) || id.StartsWith("type:", StringComparison.Ordinal))
            return id;

        if (LooksLikePacketId(id) && _repository.ContainsPacket(id))
            return PacketId(id);

        return TypeId(ResolveKnownTypeName(id));
    }

    private string ResolveKnownTypeName(string typeName)
    {
        if (_knownTypes.Contains(typeName)) return typeName;
        return _knownTypeAliases.TryGetValue(typeName, out var resolved) ? resolved : typeName;
    }

    private static string? FirstPath(Dictionary<ProtocolRange, ProtodefType?> history) =>
        history.Values.FirstOrDefault(type => type is not null)?.Path;

    private static bool MatchesKind(string value, string? targetKind) =>
        string.IsNullOrWhiteSpace(targetKind) || string.Equals(value, targetKind, StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePacketId(string id) => id.Count(ch => ch == '.') == 2;

    private static string? TryGetSwitchCase(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var last = path.Split('.').LastOrDefault();
        return last is "type" or "countType" or "default" or null ? null : last;
    }

    private static string ShortTypeName(string typeName)
    {
        var index = typeName.LastIndexOf('.');
        return index >= 0 ? typeName[(index + 1)..] : typeName;
    }

    private static int KindOrder(string kind) => kind switch
    {
        "packet" => 0,
        "type" => 0,
        "shape" => 1,
        "native" => 2,
        _ => 9
    };

    private static string RegistryId(string packetNamespace) => $"registry:{packetNamespace}";
    private static string PacketId(string id) => $"packet:{id}";
    private static string PacketId(string packetNamespace, string packetName) => $"packet:{packetNamespace}.{packetName}";
    private static string PacketNamespaceFromId(string id) => string.Join('.', id["packet:".Length..].Split('.').Take(2));
    private static string PacketNameFromId(string id) => id[(id.LastIndexOf('.') + 1)..];
    private static string TypeId(string typeName) => $"type:{typeName}";
    private static string NativeId(string typeName) => $"native:{typeName}";
    private static string ShapeId(string kindName) => $"shape:{kindName}";
    private static string? EmptyToNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    private static string RangeLabel(ProtocolRange range) => range.From == range.To ? range.From.ToString() : $"{range.From}-{range.To}";

    private sealed record UsageOwner(
        string Id,
        string Kind,
        string Path,
        Dictionary<ProtocolRange, ProtodefType?> History);

    private sealed record UsageTarget(string Id, string Kind, string Path, string Label);
}

public sealed record UsageRecord(
    string OwnerId,
    string OwnerKind,
    string OwnerPath,
    string SourcePath,
    string TargetId,
    string TargetKind,
    string TargetPath,
    string TargetLabel,
    string VersionRange,
    string? FieldPath,
    string? Case);

public sealed record UsageSummary(
    string TargetId,
    string TargetKind,
    string Label,
    string Path,
    int UsageCount,
    int OwnerCount,
    IReadOnlyCollection<string> VersionRanges);

public sealed record DependencySummary(
    string TargetId,
    string TargetKind,
    string Label,
    string Path,
    int UsageCount,
    IReadOnlyCollection<string> VersionRanges,
    IReadOnlyCollection<string> FieldPaths);

public sealed record DependencyResult(
    string Id,
    string Kind,
    string Path,
    IReadOnlyCollection<DependencySummary> Dependencies);
