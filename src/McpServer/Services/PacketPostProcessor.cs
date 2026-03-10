using System;
using System.Collections.Generic;
using System.Text;
using McpServer.Repositories;

namespace McpServer.Services;

public static class PacketPostProcessor
{
    private const string Usings =
        "using McProtoNet.Protocol;\nusing McProtoNet.Protocol.Attributes;\nusing McProtoNet.Serialization;";

    public static string Process(string code, PacketDefinition packet, ProtocolRange supportedRange)
    {
        var nsParts = packet.Namespace.Split('.');
        var iface = nsParts.Length > 1 && nsParts[1] == "toServer"
            ? "IClientPacket"
            : "IServerPacket";

        var attributes = BuildAttributes(packet, supportedRange);
        return code
            .Replace("{{usages}}", Usings)
            .Replace("{{attributes}}", attributes)
            .Replace(": IPacket", $": {iface}");
    }

    private static string BuildAttributes(PacketDefinition packet, ProtocolRange supportedRange)
    {
        // "play.toServer" → state=Play, dir=Serverbound
        var nsParts = packet.Namespace.Split('.');
        var state = nsParts[0] switch
        {
            "play" => "PacketState.Play",
            "login" => "PacketState.Login",
            "status" => "PacketState.Status",
            "configuration" => "PacketState.Configuration",
            "handshaking" => "PacketState.Handshaking",
            var s => throw new ArgumentException($"Unknown packet namespace: {s}")
        };
        var direction = nsParts.Length > 1 && nsParts[1] == "toServer"
            ? "PacketDirection.Serverbound"
            : "PacketDirection.Clientbound";

        var sb = new StringBuilder();
        sb.AppendLine($"[PacketInfo(\"{packet.Name}\", {state}, {direction})]");

        foreach (var (range, type) in packet.History)
        {
            if (type is null) continue;

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

    // Merge adjacent PacketIdEntry items with the same hex ID
    private static List<PacketIdEntry> CompressPacketIds(List<PacketIdEntry> entries)
    {
        if (entries.Count == 0) return entries;

        // Sort by From
        var sorted = new List<PacketIdEntry>(entries);
        sorted.Sort((a, b) => a.Range.From.CompareTo(b.Range.From));

        var result = new List<PacketIdEntry>(sorted.Count);
        var current = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];
            if (next.Id == current.Id && next.Range.From == current.Range.To + 1)
            {
                // Extend current range
                current = new PacketIdEntry(new ProtocolRange(current.Range.From, next.Range.To), current.Id);
            }
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