using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using McpServer.Repositories;
using Protodef;
using Scriban;

namespace McpServer.Services;

public static class PacketPostProcessor
{
    private const string Usings =
        "using McProtoNet.Protocol;\nusing McProtoNet.Protocol.Attributes;\nusing McProtoNet.Serialization;";

    public static string Process(string code, PacketDefinition packet, ProtocolRange supportedRange)
    {
        var nsParts = packet.Namespace.Split('.');
        var isServerbound = nsParts.Length > 1 && nsParts[1] == "toServer";
        var iface = isServerbound ? "IClientPacket" : "IServerPacket";

        var statePart = nsParts[0] switch
        {
            "play"          => "Play",
            "login"         => "Login",
            "status"        => "Status",
            "configuration" => "Configuration",
            "handshaking"   => "Handshaking",
            var s           => throw new ArgumentException($"Unknown state: {s}")
        };
        var dirPart   = isServerbound ? "Serverbound" : "Clientbound";
        var nsDecl    = $"namespace McProtoNet.Protocol.Packets.{statePart}.{dirPart};";

        var attributes = BuildAttributes(packet, supportedRange);

        return Template.ParseLiquid(code).Render(new
        {
            usages         = Usings,
            namespace_decl = nsDecl,
            attributes     = attributes,
            @interface     = iface
        });
    }

    private static string BuildAttributes(PacketDefinition packet, ProtocolRange supportedRange)
    {
        var nsParts = packet.Namespace.Split('.');
        var state = nsParts[0] switch
        {
            "play"          => "PacketState.Play",
            "login"         => "PacketState.Login",
            "status"        => "PacketState.Status",
            "configuration" => "PacketState.Configuration",
            "handshaking"   => "PacketState.Handshaking",
            var s           => throw new ArgumentException($"Unknown packet namespace: {s}")
        };
        var direction = nsParts.Length > 1 && nsParts[1] == "toServer"
            ? "PacketDirection.Serverbound"
            : "PacketDirection.Clientbound";

        var sb = new StringBuilder();
        sb.AppendLine($"[PacketInfo(\"{packet.Name}\", {state}, {direction})]");

        foreach (var range in CompressProtocolSupport(packet.History, supportedRange))
        {
            var from = range.From == supportedRange.From
                ? "MinecraftVersion.StartProtocol"
                : range.From.ToString();
            var to = range.To == supportedRange.To
                ? "MinecraftVersion.LatestProtocol"
                : range.To.ToString();

            sb.AppendLine($"[ProtocolSupport({from}, {to})]");
        }

        foreach (var entry in CompressPacketIds(packet.PacketIds))
        {
            var from = entry.Range.From == supportedRange.From
                ? "MinecraftVersion.StartProtocol"
                : entry.Range.From.ToString();
            var to = entry.Range.To == supportedRange.To
                ? "MinecraftVersion.LatestProtocol"
                : entry.Range.To.ToString();

            sb.AppendLine($"[PacketId({from}, {to}, 0x{entry.Id:X2})]");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Replaces numeric version keys in a JsonObject with "first"/"last" aliases
    /// based on the supported protocol range.
    /// </summary>
    public static void ApplyVersionAliases(JsonObject obj, ProtocolRange supportedRange)
    {
        var first = supportedRange.From.ToString();
        var last  = supportedRange.To.ToString();

        for (var i = 0; i < obj.Count; i++)
        {
            var node   = obj.GetAt(i);
            var newKey = node.Key.Replace(first, "first").Replace(last, "last");
            if (newKey != node.Key)
                obj.SetAt(i, newKey, node.Value?.DeepClone());
        }
    }

    private static List<ProtocolRange> CompressProtocolSupport(
        Dictionary<ProtocolRange, ProtodefType?> history,
        ProtocolRange supportedRange)
    {
        var ranges = new List<ProtocolRange>();
        foreach (var (range, type) in history)
            if (type is not null) ranges.Add(range);

        if (ranges.Count == 0) return ranges;

        ranges.Sort((a, b) => a.From.CompareTo(b.From));

        var result = new List<ProtocolRange>(ranges.Count);
        var current = ranges[0];

        for (var i = 1; i < ranges.Count; i++)
        {
            var next = ranges[i];
            if (next.From == current.To + 1)
                current = new ProtocolRange(current.From, next.To);
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }

    private static List<PacketIdEntry> CompressPacketIds(List<PacketIdEntry> entries)
    {
        if (entries.Count == 0) return entries;

        var sorted = new List<PacketIdEntry>(entries);
        sorted.Sort((a, b) => a.Range.From.CompareTo(b.Range.From));

        var result = new List<PacketIdEntry>(sorted.Count);
        var current = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];
            if (next.Id == current.Id && next.Range.From == current.Range.To + 1)
                current = new PacketIdEntry(new ProtocolRange(current.Range.From, next.Range.To), current.Id);
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }
}
