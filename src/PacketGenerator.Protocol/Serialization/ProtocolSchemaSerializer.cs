using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using PacketGenerator.Protocol.Repository;
using Protodef;

namespace PacketGenerator.Protocol.Serialization;

public static class ProtocolSchemaSerializer
{
    public static JsonNode ToJsonNode(
        Dictionary<ProtocolRange, ProtodefType?> history,
        ProtocolRange supportedRange)
    {
        var json = JsonSerializer.SerializeToNode(history, ProtodefType.DefaultJsonOptions)!;
        ApplyVersionAliases(json.AsObject(), supportedRange);
        return json;
    }

    public static string Serialize(JsonNode json, OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Toon => ToonSerializer.Encode(json),
            _ => JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            })
        };
    }

    public static void ApplyVersionAliases(JsonObject obj, ProtocolRange supportedRange)
    {
        var first = supportedRange.From.ToString();
        var last = supportedRange.To.ToString();

        for (var i = 0; i < obj.Count; i++)
        {
            var node = obj.GetAt(i);
            var newKey = node.Key.Replace(first, "first").Replace(last, "last");
            if (newKey != node.Key)
                obj.SetAt(i, newKey, node.Value?.DeepClone());
        }
    }
}
