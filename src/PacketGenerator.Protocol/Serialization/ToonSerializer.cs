using System.Text.Json;
using System.Text.Json.Nodes;
using Cysharp.AI;

namespace PacketGenerator.Protocol.Serialization;

public static class ToonSerializer
{
    public static string Encode(JsonNode? json)
    {
        using var document = JsonDocument.Parse(json?.ToJsonString() ?? "null");
        return ToonEncoder.Encode(document.RootElement);
    }
}

