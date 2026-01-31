using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Humanizer;
using ProtoCore;
using Protodef;
using Protodef.Enumerable;
using Protodef.Primitive;

namespace McpServer.Repositories;

public class PacketDefinition
{
    public string Namespace { get; set; }
    public string Name { get; set; }
    public Dictionary<ProtocolRange, ProtodefType?> History => _history.History;

    private TypeHistory _history;

    public PacketDefinition(string ns,string name, TypeHistory history)
    {
        Namespace = ns;
        Name = name;
        _history = history;
    }

    internal void Merge(TypeHistory history)
    {
        _history = HistoryBuilder.MergeTypeHistories(_history, history);
    }
}

public class ProtocolRepository : IProtocolRepository
{
    private readonly ProtocolRange _range;
    private readonly ProtocolMap _map;
    private IReadOnlyDictionary<string, TypeHistory> _types;

    private readonly Dictionary<string, Dictionary<string, PacketDefinition>> _packets = new();

    public ProtocolRepository(
        ProtocolRange range,
        ProtocolMap map,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        _range = range;
        _map = map;
        _types = types;

        string gg = "PascalCase".Underscore();

        foreach (var kv in types)
        {
            if (kv.Key.EndsWith("packet", StringComparison.OrdinalIgnoreCase))
            {
                var ns = GetNamespace(kv.Value.Id);
                Test(ns, kv.Value.History, types);
            }
        }
    }

    private void Test(
        string ns,
        Dictionary<ProtocolRange, ProtodefType?> ranges,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        Dictionary<string, PacketDefinition> packets = new();
        foreach (var kv in ranges)
        {
            if (kv.Value is not null)
            {
                var cont = (ProtodefContainer)kv.Value;
                var idMapper = cont.GetFiled<ProtodefMapper>("name");
                var nameSwitch = cont.GetFiled<ProtodefSwitch>("params");

                var names = idMapper.Mappings.Values.ToArray();


                if (nameSwitch.Fields is null)
                    throw new InvalidOperationException("Null");

                foreach (var p in nameSwitch.Fields)
                {
                    TypeHistory finded;
                    
                    string name = p.Key.Pascalize();
                    if (p.Value.IsVoid())
                    {
                        finded = new TypeHistory
                        {
                            Name = name,
                            Id = p.Key,
                            History =
                            {
                                { kv.Key, new ProtodefVoid() }
                            }
                        };
                    }
                    else
                    {
                        var custom = (p.Value as ProtodefCustomType)!.Name;
                        finded = FindType(ns, custom, types);
                    }

                    
                    
                    if (packets.ContainsKey(p.Key))
                    {
                        if (packets[p.Key].Name != finded.Name)
                        {
                            packets[p.Key].Merge(finded);
                        }
                    }
                    else
                    {
                        packets[p.Key] = new PacketDefinition(ns,name, finded);
                    }
                }
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
        {
            if (types.TryGetValue(key, out var type))
            {
                return type;
            }
        }
        throw new KeyNotFoundException($"No type found with name {name}");
    }

    public Dictionary<string, Dictionary<string, PacketDefinition>> GetPackets()
    {
        return _packets;
    }

    public PacketDefinition GetPacket(string id)
    {
        var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid packet id {id}");
        }

        var ns = $"{parts[0]}.{parts[1]}";
        return GetPacket(ns, parts[2]);
    }

    public PacketDefinition GetPacket(string nameSpace, string name)
    {
        return _packets[nameSpace][name];
    }

    private static string GetNamespace(string id)
    {
        return id[..id.LastIndexOf('.')];
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
}