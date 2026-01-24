using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Humanizer;
using Microsoft.Extensions.Hosting;
using ProtoCore;
using Protodef;

namespace McpServer;

public static class HistoryBuilder
{
    public static IReadOnlyDictionary<string, TypeHistory> Build(ProtocolMap map)
    {
        var pathes = GetAllTypes(map);
        var histories = pathes
            .Select(x => BuildHistory(map, x));
        histories = RemoveDuplicates(histories);
        var dict = histories.ToFrozenDictionary(x => x.Id, v => v);

        return dict;
    }

    private static IEnumerable<TypeHistory> RemoveDuplicates(IEnumerable<TypeHistory> histories)
    {
        return histories
            .GroupBy(x => x.Id, comparer: StringComparer.OrdinalIgnoreCase)
            .Select(x => MergeTypeHistories(x.ToArray()));
    }
    
    private static TypeHistory BuildHistory(ProtocolMap map, string path)
    {
        var name = NameFromPath(path).Pascalize();
        var resolved = ResolveType(map, path).ToArray();

        return new TypeHistory
        {
            Id = path,
            Name = name,
            History = CollapseByVersion(resolved, EqualTwoStructure)
        };
    }
    
    private static Dictionary<int, ProtodefType?> ExpandHistory(
        Dictionary<ProtocolRange, ProtodefType?> history)
    {
        var result = new Dictionary<int, ProtodefType?>();

        foreach (var (range, type) in history)
        {
            for (int v = range.From; v <= range.To; v++)
            {
                result[v] = type;
            }
        }

        return result;
    }

    public static TypeHistory MergeTypeHistories(TypeHistory[] histories)
    {
        var result = histories[0];
        for (int i = 1; i < histories.Length; i++)
        {
            result = MergeTypeHistories(result, histories[i]);
        }
        return result;
    }
    
    public static TypeHistory MergeTypeHistories(
        TypeHistory a,
        TypeHistory b)
    {
        
        var name = a.Name.Pascalize(); 

        var va = ExpandHistory(a.History);
        var vb = ExpandHistory(b.History);

        var merged = new SortedDictionary<int, ProtodefType?>();

        foreach (var version in va.Keys.Union(vb.Keys))
        {
            va.TryGetValue(version, out var ta);
            vb.TryGetValue(version, out var tb);

            if (ta is null)
            {
                merged[version] = tb;
            }
            else if (tb is null)
            {
                merged[version] = ta;
            }
            else if (EqualTwoStructure(ta, tb))
            {
                merged[version] = ta;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Type conflict at version {version}");
            }
        }

        var collapsed =
            CollapseByVersion(
                merged,
                EqualTwoStructure
            );

        return new TypeHistory
        {
            Id = a.Id.Pascalize(),
            Name = name,
            History = collapsed
        };
    }
    
    private static Dictionary<ProtocolRange, T?>
        CollapseByVersion<T>(
            IEnumerable<KeyValuePair<int, T?>> source,
            Func<T?, T?, bool> equals)
    {
        var result = new Dictionary<ProtocolRange, T?>();

        using var e = source.OrderBy(x => x.Key).GetEnumerator();
        if (!e.MoveNext())
            return result;

        int from = e.Current.Key;
        int to = from;
        T? current = e.Current.Value;

        while (e.MoveNext())
        {
            if (equals(current, e.Current.Value))
            {
                to = e.Current.Key;
            }
            else
            {
                result.Add(new ProtocolRange(from, to), current);
                from = to = e.Current.Key;
                current = e.Current.Value;
            }
        }

        result.Add(new ProtocolRange(from, to), current);
        return result;
    }


    private static bool EqualTwoStructure(ProtodefType? a, ProtodefType? b)
    {
        if (a is null && b is null)
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }
        return a.Equals(b);
    }

    private static IEnumerable<KeyValuePair<int, ProtodefType?>> ResolveType(
        ProtocolMap map,
        string path)
    {
        return
            from item in map.Protocols
            let type = item.Value.Protocol!.GetByPath(path)
            select new KeyValuePair<int, ProtodefType?>(item.Key, type);
    }

    private static string[] GetAllTypes(ProtocolMap map)
    {
        return map.Protocols.Values
            .Select(x =>
                x.Protocol!.EnumerateTypes()
                    .RemoveNative()
                    .Select(y => y.Value.Path))
            .SelectMany(x => x)
            .ToHashSet()
            .ToArray();
    }

    public static string NameFromPath(string path)
    {
        var strings = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (strings.Length > 1)
        {
            return strings[^1];
        }

        return strings[0];
    }

    private static string PascalizePath(string path)
    {
        var strings = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        var last = strings[^1].Pascalize();
        strings[^1] = last;
        return string.Join('.', strings);
    }
}