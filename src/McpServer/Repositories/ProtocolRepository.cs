using System;
using System.Collections.Generic;
using System.Linq;
using ProtoCore;
using Protodef;
using Protodef.Enumerable;

namespace McpServer.Repositories;

public class ProtocolRepository : IProtocolRepository
{
    private readonly ProtocolRange _range;
    private readonly ProtocolMap _map;
    private IReadOnlyDictionary<string, TypeHistory> _types;

    public ProtocolRepository(
        ProtocolRange range,
        ProtocolMap map,
        IReadOnlyDictionary<string, TypeHistory> types)
    {
        _range = range;
        _map = map;
        _types = types;


        foreach (var kv in types)
        {
            if (kv.Key.EndsWith("packet", StringComparison.OrdinalIgnoreCase))
            {
                
            }
        }
    }

    private static void Test(Dictionary<ProtocolRange, ProtodefType?> ranges)
    {
        foreach (var kv in ranges)
        {
            if (kv.Value is not null)
            {
                var cont = (ProtodefContainer)kv.Value;
                var idMapper = cont.GetFiled<ProtodefMapper>("name");
                
            }
        }
    }

    private static string GetNamespace(string id)
    {
        return id[(id.LastIndexOf('.') + 1)..];
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

    public IEnumerable<string> GetPackets()
    {
        var count = "packet".Length;
        return
            from key in _types.Keys
            let name = HistoryBuilder.NameFromPath(key)
            where name.StartsWith("packet", StringComparison.OrdinalIgnoreCase) && name.Length > count
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