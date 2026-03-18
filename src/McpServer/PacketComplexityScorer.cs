using System.Collections.Generic;
using System.Linq;
using Protodef;
using Protodef.Enumerable;

namespace McpServer;

/// <summary>
/// Computes a structural complexity score for a packet based on its version history.
/// Used for model-tier routing: simple packets → cheap model, complex → strong model.
///
/// Scoring:
///   +10  per non-null version range   (each range = one code path)
///   +2   per field in the largest range
///   +20  if any nested array detected  (array of array)
///   +5   if any mapper detected        (enum-like type)
///   +15  if any switch detected        (complex branching)
///   +10  if any option detected        (nullable field)
///   +15  per type conflict             (same field name, different type across ranges)
/// </summary>
public static class PacketComplexityScorer
{
    public static int Compute(Dictionary<ProtocolRange, ProtodefType?> history)
    {
        var nonNull = history.Where(kv => kv.Value is not null).ToList();
        if (nonNull.Count == 0) return 0;

        int score = 0;

        // 1. Each range = one switch branch
        score += nonNull.Count * 10;

        // 2. Fields in the heaviest range
        var maxFields = nonNull
            .Select(kv => kv.Value is ProtodefContainer c ? c.Fields.Count : 0)
            .DefaultIfEmpty(0).Max();
        score += maxFields * 2;

        // 3. Special type features
        bool hasMapper = false, hasSwitch = false, hasOption = false, hasNestedArray = false;
        foreach (var (_, type) in nonNull)
        {
            hasMapper      |= HasType<ProtodefMapper>(type!);
            hasSwitch      |= HasType<ProtodefSwitch>(type!);
            hasOption      |= HasType<ProtodefOption>(type!);
            hasNestedArray |= HasNestedArray(type!);
        }

        if (hasNestedArray) score += 20;
        if (hasMapper)      score += 5;
        if (hasSwitch)      score += 15;
        if (hasOption)      score += 10;

        // 4. Type conflicts between ranges (same field, different type)
        score += CountTypeConflicts(nonNull) * 15;

        return score;
    }

    private static bool HasType<T>(ProtodefType type) where T : ProtodefType
    {
        if (type is T) return true;
        foreach (var (_, child) in type.Children)
            if (HasType<T>(child)) return true;
        return false;
    }

    private static bool HasNestedArray(ProtodefType type)
    {
        if (type is ProtodefArray)
        {
            foreach (var (_, child) in type.Children)
                if (child is ProtodefArray) return true;
        }
        foreach (var (_, child) in type.Children)
            if (HasNestedArray(child)) return true;
        return false;
    }

    private static int CountTypeConflicts(List<KeyValuePair<ProtocolRange, ProtodefType?>> ranges)
    {
        if (ranges.Count < 2) return 0;

        var fieldMaps = ranges
            .Select(kv => kv.Value is ProtodefContainer c
                ? c.Fields.ToDictionary(f => f.Name, f => f.Type)
                : new Dictionary<string, ProtodefType>())
            .ToList();

        return fieldMaps
            .SelectMany(d => d.Keys)
            .Distinct()
            .Count(name =>
            {
                var types = fieldMaps
                    .Where(d => d.ContainsKey(name))
                    .Select(d => d[name])
                    .ToList();
                return types.Count > 1 && types.Any(t => !t.Equals(types[0]));
            });
    }
}
