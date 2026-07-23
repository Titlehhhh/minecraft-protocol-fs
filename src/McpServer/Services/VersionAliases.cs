using System.Text.Json.Nodes;
using PacketGenerator.Protocol.Repository;

namespace McpServer.Services;

public static class VersionAliases
{
    /// <summary>
    /// Replaces numeric version keys in a JsonObject with "first"/"last" aliases
    /// based on the supported protocol range.
    /// </summary>
    public static void Apply(JsonObject obj, ProtocolRange supportedRange)
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
}
