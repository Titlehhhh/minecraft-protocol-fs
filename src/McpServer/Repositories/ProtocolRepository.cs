using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using ProtoCore;
using Protodef;
using Protodef.Enumerable;
using Protodef.Primitive;

namespace McpServer.Repositories;

public record PacketIdEntry(ProtocolRange Range, int Id);

public class PacketDefinition
{
    private TypeHistory _history;

    public PacketDefinition(string ns, string name, TypeHistory history)
    {
        Namespace = ns;
        Name = name;
        _history = history;
    }

    public string Namespace { get; set; }
    public string Name { get; set; }
    public Dictionary<ProtocolRange, ProtodefType?> History => _history.History;

    /// <summary>Hex packet ID per mapper version range. Multiple entries if ID changed between versions.</summary>
    public List<PacketIdEntry> PacketIds { get; } = new();

    internal void Merge(TypeHistory history)
    {
        _history = HistoryBuilder.MergeTypeHistories(_history, history);
    }
}

public class ProtocolRepository : IProtocolRepository
{
    private readonly ProtocolMap _map;

    private readonly Dictionary<string, Dictionary<string, PacketDefinition>> _packets = new();
    private readonly ProtocolRange _range;
    private readonly IReadOnlyDictionary<string, TypeHistory> _types;

    public ProtocolRepository(
        ProtocolRange range,
        ProtocolMap map,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        _range = range;
        _map = map;
        _types = types;

        foreach (var kv in types)
            if (kv.Key.EndsWith("packet", StringComparison.OrdinalIgnoreCase))
            {
                var ns = GetNamespace(kv.Value.Id);
                BuildPackets(ns, kv.Value.History, types);
            }
    }

    public Dictionary<string, Dictionary<string, PacketDefinition>> GetPackets()
    {
        return _packets;
    }

    public PacketDefinition GetPacket(string id)
    {
        var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3) throw new ArgumentException($"Invalid packet id {id}");

        var ns = $"{parts[0]}.{parts[1]}";
        return GetPacket(ns, parts[2]);
    }

    public PacketDefinition GetPacket(string nameSpace, string name)
    {
        return _packets[nameSpace][name];
    }

    public ProtocolRange GetSupportedProtocols()
    {
        return _range;
    }

    public IEnumerable<string> GetTypes()
    {
        return
            from key in _types.Keys
            let name = HistoryBuilder.NameFromPath(key)
            where !name.StartsWith("packet", StringComparison.OrdinalIgnoreCase)
            select key;
    }

    public IEnumerable<string> GetPacketMappers()
    {
        return
            from key in _types.Keys
            let name = HistoryBuilder.NameFromPath(key)
            where name.Equals("packet", StringComparison.OrdinalIgnoreCase)
            select key;
    }

    public TypeHistory GetTypeHistory(string id)
    {
        return _types[id];
    }

    public bool ContainsPacket(string id)
    {
        try
        {
            GetPacket(id);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private void BuildPackets(
        string ns,
        Dictionary<ProtocolRange, ProtodefType?> ranges,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        Dictionary<string, PacketDefinition> packets = new();

        foreach (var kv in ranges)
        {
            if (kv.Value is null) continue;

            var cont = (ProtodefContainer)kv.Value;
            var idMapper = cont.GetFiled<ProtodefMapper>("name");
            var nameSwitch = cont.GetFiled<ProtodefSwitch>("params");

            // Reverse lookup: canonicalName → hexId for this range
            var nameToHexId = idMapper.Mappings
                .Where(m => m.Value is not null)
                .ToDictionary(
                    m => m.Value!, // "face_player"
                    m => Convert.ToInt32(m.Key, 16)); // 0x3F

            if (nameSwitch.Fields is null)
                throw new InvalidOperationException("nameSwitch.Fields is null");

            foreach (var p in nameSwitch.Fields)
            {
                TypeHistory found;

                var name = p.Key.Pascalize();
                if (p.Value.IsVoid())
                {
                    found = new TypeHistory
                    {
                        Name = name,
                        Id = p.Key,
                        History = { { kv.Key, new ProtodefVoid() } }
                    };
                }
                else
                {
                    var custom = (p.Value as ProtodefCustomType)!.Name;
                    found = FindType(ns, custom, types);
                }

                if (!packets.ContainsKey(p.Key))
                    packets[p.Key] = new PacketDefinition(ns, name, found);
                else if (packets[p.Key].Name != found.Name)
                    packets[p.Key].Merge(found);

                // Store hex ID for this version range
                if (nameToHexId.TryGetValue(p.Key, out var hexId))
                    packets[p.Key].PacketIds.Add(new PacketIdEntry(kv.Key, hexId));
            }
        }

        _packets[ns] = packets;
    }

    private TypeHistory FindType(
        string ns,
        string name,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        var ids = new[]
        {
            $"{ns}.{name.Underscore()}",
            $"{ns}.{name.Pascalize()}",
            $"{name.Underscore()}",
            $"{name.Pascalize()}"
        };

        foreach (var key in ids)
            if (types.TryGetValue(key, out var type))
                return type;

        throw new KeyNotFoundException($"No type found with name {name}");
    }

    private static string GetNamespace(string id)
    {
        return id[..id.LastIndexOf('.')];
    }
}