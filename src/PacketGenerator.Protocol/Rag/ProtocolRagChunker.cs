using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PacketGenerator.Protocol.Complexity;
using PacketGenerator.Protocol.Repository;
using Protodef;
using Protodef.Enumerable;

namespace PacketGenerator.Protocol.Rag;

public sealed partial class ProtocolRagChunker
{
    private static readonly HashSet<string> StructuralKinds = new(StringComparer.Ordinal)
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

    private readonly IProtocolRepository _repository;
    private readonly ComplexityThresholds _thresholds;
    private readonly ProtocolRagChunkOptions _options;

    public ProtocolRagChunker(
        IProtocolRepository repository,
        ComplexityThresholds? thresholds = null,
        ProtocolRagChunkOptions? options = null)
    {
        _repository = repository;
        _thresholds = thresholds ?? new ComplexityThresholds();
        _options = options ?? new ProtocolRagChunkOptions();
    }

    public ProtocolRagChunkSet BuildChunks(string kind = "all", string? filter = null)
    {
        var chunks = new List<ProtocolRagChunk>();

        if (kind is "all" or "packet")
        {
            foreach (var (ns, packets) in _repository.GetPackets().OrderBy(x => x.Key, StringComparer.Ordinal))
            foreach (var (packetName, packet) in packets.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var ownerId = $"{ns}.{packetName}";
                if (!MatchesFilter(ownerId, filter)) continue;
                chunks.AddRange(BuildPacketChunks(ownerId, packet));
            }
        }

        if (kind is "all" or "type")
        {
            foreach (var typeId in _repository.GetTypes().OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!MatchesFilter(typeId, filter)) continue;
                chunks.AddRange(BuildTypeChunks(typeId));
            }
        }

        return new ProtocolRagChunkSet(chunks);
    }

    public IReadOnlyList<ProtocolRagChunk> BuildPacketChunks(string ownerId)
    {
        return BuildPacketChunks(ownerId, _repository.GetPacket(ownerId));
    }

    public IReadOnlyList<ProtocolRagChunk> BuildTypeChunks(string ownerId)
    {
        var history = _repository.GetTypeHistory(ownerId);
        var score = PacketComplexityScorer.Compute(history.History);
        return BuildOwnerChunks(
            ownerKind: "type",
            ownerId: ownerId,
            displayName: history.Name,
            history: history.History,
            tier: _thresholds.Classify(score).ToLabel());
    }

    private IReadOnlyList<ProtocolRagChunk> BuildPacketChunks(string ownerId, PacketDefinition packet)
    {
        var score = PacketComplexityScorer.Compute(packet.History);
        return BuildOwnerChunks(
            ownerKind: "packet",
            ownerId: ownerId,
            displayName: packet.Name,
            history: packet.History,
            tier: _thresholds.Classify(score).ToLabel());
    }

    private IReadOnlyList<ProtocolRagChunk> BuildOwnerChunks(
        string ownerKind,
        string ownerId,
        string displayName,
        Dictionary<ProtocolRange, ProtodefType?> history,
        string tier)
    {
        var chunks = new List<ProtocolRagChunk>();
        var ownerFields = StableUnique(history.Values.Where(x => x is not null).SelectMany(x => CollectFields(x!)));
        var ownerKinds = StableUnique(history.Values.Where(x => x is not null).SelectMany(x => CollectKinds(x!)));
        var categories = ClassifyCategories(history, ownerFields, ownerKinds);
        var hints = ClassifySemantic(ownerId, ownerFields, ownerKinds);

        chunks.Add(MakeChunk(
            ownerKind,
            ownerId,
            "owner-summary",
            "summary",
            "all",
            ownerFields,
            ownerKinds,
            categories,
            hints,
            $"{ownerKind} {ownerId} name={displayName} tier={tier} ranges={string.Join(", ", history.Keys.Select(RangeLabel))}"));

        foreach (var (range, rawType) in history.OrderBy(x => x.Key.From).ThenBy(x => x.Key.To))
        {
            var rangeLabel = RangeLabel(range);
            if (rawType is null)
            {
                chunks.Add(MakeChunk(
                    ownerKind,
                    ownerId,
                    "version-range",
                    $"range/{rangeLabel}",
                    rangeLabel,
                    [],
                    ["null"],
                    [.. categories, "version-null"],
                    hints,
                    $"range {rangeLabel} is null"));
                continue;
            }

            var type = rawType.CreatePrimitiveResolvedCopy();
            var fields = StableUnique(CollectFields(type));
            var kinds = StableUnique(CollectKinds(type));
            var topFields = type is ProtodefContainer container ? container.Fields : [];

            chunks.Add(MakeChunk(
                ownerKind,
                ownerId,
                "version-range",
                $"range/{rangeLabel}",
                rangeLabel,
                fields,
                kinds,
                categories,
                hints,
                $"range {rangeLabel} root={DescribeType(type)} topFields={string.Join(", ", topFields.Select(f => f.Name))}"));

            AddFieldBatches(chunks, ownerKind, ownerId, rangeLabel, topFields, categories, hints);
            VisitNode(chunks, ownerKind, ownerId, rangeLabel, type, $"range/{rangeLabel}", categories, hints, depth: 0);
        }

        return chunks
            .GroupBy(chunk => chunk.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(chunk => chunk.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private void AddFieldBatches(
        List<ProtocolRagChunk> chunks,
        string ownerKind,
        string ownerId,
        string rangeLabel,
        IReadOnlyList<ProtodefContainerField> fields,
        string[] categories,
        string[] hints)
    {
        if (fields.Count <= _options.EffectiveFieldBatchSize) return;

        for (var offset = 0; offset < fields.Count; offset += _options.EffectiveFieldBatchSize)
        {
            var batch = fields.Skip(offset).Take(_options.EffectiveFieldBatchSize).ToArray();
            chunks.Add(MakeChunk(
                ownerKind,
                ownerId,
                "field-batch",
                $"range/{rangeLabel}/fields-{offset / _options.EffectiveFieldBatchSize + 1}",
                rangeLabel,
                StableUnique(batch.Select(f => f.Name ?? "_")),
                StableUnique(batch.SelectMany(f => CollectKinds(f.Type))),
                [.. categories, "field-batch"],
                hints,
                string.Join("; ", batch.Select(f => $"{f.Name}: {DescribeType(f.Type)}"))));
        }
    }

    private void VisitNode(
        List<ProtocolRagChunk> chunks,
        string ownerKind,
        string ownerId,
        string rangeLabel,
        ProtodefType type,
        string path,
        string[] ownerCategories,
        string[] ownerHints,
        int depth)
    {
        if (depth > 128) return;

        var kind = ProtodefTypeAnalyzer.GetKindName(type);
        var fields = StableUnique(CollectFields(type));
        var kinds = StableUnique(CollectKinds(type));
        var categories = StructuralKinds.Contains(kind)
            ? StableUnique([.. ownerCategories, $"has-{kind}"])
            : ownerCategories;

        if (ShouldEmitNode(type, path))
        {
            chunks.Add(MakeChunk(
                ownerKind,
                ownerId,
                ChunkKind(kind),
                path,
                rangeLabel,
                fields,
                kinds,
                categories,
                ownerHints,
                DescribeType(type)));
        }

        AddCaseBatches(chunks, ownerKind, ownerId, rangeLabel, type, path, categories, ownerHints);

        foreach (var (childKey, child) in type.Children)
        {
            var childPath = string.IsNullOrWhiteSpace(childKey)
                ? $"{path}/_"
                : $"{path}/{EscapePathPart(childKey)}";
            VisitNode(chunks, ownerKind, ownerId, rangeLabel, child, childPath, ownerCategories, ownerHints, depth + 1);
        }
    }

    private void AddCaseBatches(
        List<ProtocolRagChunk> chunks,
        string ownerKind,
        string ownerId,
        string rangeLabel,
        ProtodefType type,
        string path,
        string[] categories,
        string[] hints)
    {
        if (type is ProtodefSwitch sw && sw.Fields is not null && sw.Fields.Count > _options.EffectiveCaseBatchSize)
        {
            AddCaseBatches(chunks, ownerKind, ownerId, rangeLabel, "switch-case-batch", path, sw.Fields.Keys, categories, hints);
        }

        if (type is ProtodefMapper mapper && mapper.Mappings.Count > _options.EffectiveCaseBatchSize)
        {
            AddCaseBatches(
                chunks,
                ownerKind,
                ownerId,
                rangeLabel,
                "mapper-case-batch",
                path,
                mapper.Mappings.Select(kv => $"{kv.Key}->{kv.Value}"),
                categories,
                hints);
        }
    }

    private void AddCaseBatches(
        List<ProtocolRagChunk> chunks,
        string ownerKind,
        string ownerId,
        string rangeLabel,
        string chunkKind,
        string path,
        IEnumerable<string> cases,
        string[] categories,
        string[] hints)
    {
        var allCases = cases.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        for (var offset = 0; offset < allCases.Length; offset += _options.EffectiveCaseBatchSize)
        {
            var batch = allCases.Skip(offset).Take(_options.EffectiveCaseBatchSize).ToArray();
            chunks.Add(MakeChunk(
                ownerKind,
                ownerId,
                chunkKind,
                $"{path}/cases-{offset / _options.EffectiveCaseBatchSize + 1}",
                rangeLabel,
                [],
                [],
                [.. categories, "case-batch"],
                hints,
                $"cases: {string.Join(", ", batch)}"));
        }
    }

    private ProtocolRagChunk MakeChunk(
        string ownerKind,
        string ownerId,
        string chunkKind,
        string path,
        string versionRange,
        IEnumerable<string> fields,
        IEnumerable<string> kinds,
        IEnumerable<string> categories,
        IEnumerable<string> semanticHints,
        string body)
    {
        var fieldList = StableUnique(fields);
        var kindList = StableUnique(kinds);
        var categoryList = StableUnique(categories);
        var hintList = StableUnique(semanticHints);
        var normalizedBody = CompactText(body, _options.EffectiveMaxBodyChars);

        var text = string.Join("\n", [
            $"owner: {ownerKind}:{ownerId}",
            $"chunk: {chunkKind}",
            $"path: {path}",
            $"version range: {versionRange}",
            $"fields: {CompactList(fieldList)}",
            $"kinds: {CompactList(kindList)}",
            $"categories: {CompactList(categoryList)}",
            $"semantic hints: {CompactList(hintList)}",
            normalizedBody
        ]);
        text = EnforceBudget(text);

        var id = $"{ownerKind}:{ownerId}#{path}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        return new ProtocolRagChunk(
            Id: id,
            OwnerKind: ownerKind,
            OwnerId: ownerId,
            ChunkKind: chunkKind,
            Path: path,
            VersionRange: versionRange,
            Text: text,
            Fields: fieldList,
            Kinds: kindList,
            Categories: categoryList,
            SemanticHints: hintList,
            RawPath: $"data/raw/{ownerKind}__{ownerId.Replace('.', '_')}.json",
            TextCharCount: text.Length,
            EstimatedTokenCount: EstimateTokens(text),
            ContentHash: hash);
    }

    private string EnforceBudget(string text)
    {
        var maxChars = Math.Min(_options.EffectiveMaxTextChars, _options.EffectiveMaxEstimatedTokens * 3);
        if (text.Length <= maxChars) return text;
        return CompactText(text, maxChars);
    }

    private static bool ShouldEmitNode(ProtodefType type, string path)
    {
        if (path.EndsWith("/type", StringComparison.Ordinal)) return true;
        var kind = ProtodefTypeAnalyzer.GetKindName(type);
        return StructuralKinds.Contains(kind);
    }

    private static string ChunkKind(string kind) => kind switch
    {
        "cus_switch" => "switch",
        _ => kind
    };

    private string CompactList(IEnumerable<string> values)
    {
        var clean = StableUnique(values.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (clean.Length <= _options.EffectiveMaxListItems) return string.Join(", ", clean);
        return string.Join(", ", clean.Take(_options.EffectiveMaxListItems)) + $", ... +{clean.Length - _options.EffectiveMaxListItems}";
    }

    private static string CompactText(string value, int maxChars)
    {
        value = Whitespace().Replace(value, " ").Trim();
        if (value.Length <= maxChars) return value;
        return value[..Math.Max(0, maxChars - 16)].TrimEnd() + " ... [truncated]";
    }

    private static int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / 3.0);

    private static string DescribeType(ProtodefType type)
    {
        var kind = ProtodefTypeAnalyzer.GetKindName(type);
        return type switch
        {
            ProtodefContainer container =>
                $"container fields={string.Join(", ", container.Fields.Select(f => $"{f.Name}:{ProtodefTypeAnalyzer.GetKindName(f.Type)}"))}",
            ProtodefSwitch sw =>
                $"switch compareTo={sw.CompareTo} cases={DescribeCases(sw.Fields?.Keys)} default={sw.Default is not null}",
            ProtodefMapper mapper =>
                $"mapper type={ProtodefTypeAnalyzer.GetKindName(mapper.Type)} cases={DescribeCases(mapper.Mappings.Select(kv => $"{kv.Key}->{kv.Value}"))}",
            ProtodefArray array =>
                $"array countType={DescribeNullable(array.CountType)} count={array.Count} item={ProtodefTypeAnalyzer.GetKindName(array.Type)}",
            ProtodefOption option =>
                $"option type={ProtodefTypeAnalyzer.GetKindName(option.Type)}",
            ProtodefBuffer buffer =>
                $"buffer countType={DescribeNullable(buffer.CountType)} count={buffer.Count} rest={buffer.Rest}",
            ProtodefBitField bitField =>
                $"bitfield fields={string.Join(", ", bitField.Select(x => $"{x.Name}:{x.Size}:{(x.Signed ? "signed" : "unsigned")}"))}",
            ProtodefLoop loop =>
                $"loop endVal={loop.EndValue} type={ProtodefTypeAnalyzer.GetKindName(loop.Type)}",
            _ => kind
        };
    }

    private static string DescribeCases(IEnumerable<string>? cases)
    {
        if (cases is null) return "";
        var values = cases.Take(32).ToArray();
        return string.Join(", ", values);
    }

    private static string DescribeNullable(ProtodefType? type) =>
        type is null ? "null" : ProtodefTypeAnalyzer.GetKindName(type);

    private static IEnumerable<string> CollectFields(ProtodefType type)
    {
        if (type is ProtodefContainer container)
            foreach (var field in container.Fields)
                if (!string.IsNullOrWhiteSpace(field.Name))
                    yield return field.Name!;

        foreach (var (_, child) in type.Children)
        foreach (var field in CollectFields(child))
            yield return field;
    }

    private static IEnumerable<string> CollectKinds(ProtodefType type)
    {
        yield return ProtodefTypeAnalyzer.GetKindName(type);
        foreach (var (_, child) in type.Children)
        foreach (var kind in CollectKinds(child))
            yield return kind;
    }

    private static string[] ClassifyCategories(
        Dictionary<ProtocolRange, ProtodefType?> history,
        string[] fields,
        string[] kinds)
    {
        var categories = new List<string>();
        if (fields.Length <= 2 && !kinds.Any(StructuralKinds.Contains))
            categories.Add("flat");
        else
            categories.Add("nested");

        if (history.Count > 1 || history.Any(x => x.Value is null))
            categories.Add("version-gated");

        foreach (var kind in kinds.Where(StructuralKinds.Contains).Distinct(StringComparer.Ordinal))
            categories.Add($"has-{kind}");

        if (kinds.Any(kind => kind.Contains("nbt", StringComparison.OrdinalIgnoreCase)))
            categories.Add("has-nbt");
        if (kinds.Any(kind => kind.Contains("slot", StringComparison.OrdinalIgnoreCase)))
            categories.Add("has-slot");

        return StableUnique(categories);
    }

    private static string[] ClassifySemantic(string ownerId, string[] fields, string[] kinds)
    {
        var fieldSet = fields.Select(x => x.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var haystack = string.Join(" ", [ownerId, .. fields, .. kinds]).ToLowerInvariant();
        var hints = new List<string>();

        if (ownerId.Contains("resource_pack", StringComparison.OrdinalIgnoreCase) ||
            (fieldSet.Contains("url") && fieldSet.Contains("hash")))
            hints.Add("resource-pack");
        if (ownerId.Contains("chat", StringComparison.OrdinalIgnoreCase) || haystack.Contains("signature"))
            hints.Add("chat");
        if (ContainsAny(haystack, "window", "slot", "inventory", "carrieditem", "clickeditem", "changedslots"))
            hints.Add("inventory");
        if (ContainsAny(haystack, "respawn", "dimension", "biome", "registry", "death", "portal"))
            hints.Add("world");
        if (ContainsAny(haystack, "team", "scoreboard", "collision", "nametag"))
            hints.Add("team");
        if (ContainsAny(haystack, "entity", "player"))
            hints.Add("entity");

        return StableUnique(hints);
    }

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(value.Contains);

    private static string[] StableUnique(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var value in values)
            if (seen.Add(value))
                result.Add(value);
        return result.ToArray();
    }

    private static bool MatchesFilter(string value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string EscapePathPart(string value) =>
        value.Replace("/", "~1", StringComparison.Ordinal).Replace("#", "~0", StringComparison.Ordinal);

    private static string RangeLabel(ProtocolRange range) =>
        range.From == range.To ? range.From.ToString() : $"{range.From}-{range.To}";

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
