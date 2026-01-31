using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Repositories;
using ModelContextProtocol.Server;
using Protodef;
using Toon.Format;

namespace McpServer.Tools;

public enum PacketFilterMode
{
    Contains,
    StartsWith,
    Exact
}

[McpServerToolType]
public static class DataTool
{
    [McpServerTool, Description(
         "Returns a list of all known protocol type identifiers. " +
         "Each identifier uniquely represents a data type defined in the protocol. " +
         "The result is intended for discovery and inspection, not for bulk data transfer."
     )]
    public static string GetTypes(IProtocolRepository repository)
    {
        return string.Join(", ", repository.GetTypes());
    }

    [McpServerTool, Description(
         "Returns a list of all known packet identifiers.\n\n" +
         "Filtering:\n" +
         "- The optional 'filter' parameter is a plain text filter, NOT a regular expression.\n" +
         "- The filter is case-insensitive.\n" +
         "- Multiple filter tokens can be provided, separated by '|'.\n" +
         "- A packet identifier is included only if it contains ALL specified tokens.\n\n" +
         "Examples:\n" +
         "- filter = \"player\" → matches PlayerMove, PlayerJoin\n" +
         "- filter = \"player|move\" → matches PlayerMove\n" +
         "- filter = \"auth|login\" → matches AuthLoginRequest\n\n" +
         "Do NOT use wildcards (*), regex syntax, or anchors. " +
         "This tool is intended for safe discovery and selection of packet identifiers."
     )]
    public static string GetPackets(
        IProtocolRepository repository,
        string? filter = null)
    {
        var packets =
            repository.GetPackets()
                .Select(x =>
                {
                    var dict = x.Value.Keys.ToArray();
                    return new KeyValuePair<string, string[]>(x.Key, dict);
                }).ToDictionary();

        if (string.IsNullOrWhiteSpace(filter))
        {
            var json = JsonSerializer.SerializeToNode(packets, ProtodefType.DefaultJsonOptions);
            return json.ToJsonString(new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        }
        else
        {
            static string Normalize(string value)
            {
                value = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");

                return new string(
                    value
                        .Where(char.IsLetterOrDigit)
                        .Select(char.ToLowerInvariant)
                        .ToArray()
                );
            }

            var tokens = filter
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .ToArray();

            packets = packets.Select(packetList =>
                {
                    var filtered = packetList.Value.Where(p =>
                    {
                        return tokens.All(t => p.Contains(t, StringComparison.OrdinalIgnoreCase));
                    }).ToArray();

                    return new KeyValuePair<string, string[]>(packetList.Key, filtered);
                })
                .Where(x => x.Value.Any())
                .ToDictionary();

            var json = JsonSerializer.SerializeToNode(packets, ProtodefType.DefaultJsonOptions);
            return json.ToJsonString(new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        }
    }

    [McpServerTool(UseStructuredContent = false), Description(
         "Returns the full versioned definition of a protocol type (or packet) identified by its id. " +
         "The definition includes structural changes across protocol versions. " +
         "The result is returned as formatted text for inspection or analysis, " +
         "not as structured data for further automated processing."
     )]
    public static string GetType(
        IProtocolRepository repository,
        string id,
        [Description(
            "Output format of the type definition. " +
            "Use 'toon' (default) for a compact, optimized, human-readable format suitable for LLM inspection. " +
            "Use 'json' for a fully expanded JSON representation intended for debugging or manual review."
        )]
        string format = "toon")
    {
        var hist = repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(hist, ProtodefType.DefaultJsonOptions);

        if (format == "toon")
        {
            return ToonEncoder.EncodeNode(json, new ToonEncodeOptions());
        }

        return json.ToJsonString(new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }
}

[McpServerResourceType]
public static class Resources
{
}