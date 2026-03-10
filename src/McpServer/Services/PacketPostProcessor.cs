using System;
using System.Text;
using McpServer.Repositories;

namespace McpServer.Services;

public static class PacketPostProcessor
{
    private const string Usings =
        "using McProtoNet.Protocol;\nusing McProtoNet.Serialization;";

    public static string Process(string code, PacketDefinition packet, ProtocolRange supportedRange)
    {
        var usings = Usings;
        var attributes = BuildAttributes(packet, supportedRange);
        return code
            .Replace("{{usages}}", usings)
            .Replace("{{attributes}}", attributes);
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

        foreach (var entry in packet.PacketIds)
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
}