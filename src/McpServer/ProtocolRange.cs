using System;
using System.Text.Json.Serialization;
using McpServer.Converters;

namespace McpServer;

[JsonConverter(typeof(ProtocolRangeJsonConverter))]
public readonly record struct ProtocolRange
{
    public ProtocolRange(int from, int to)
    {
        if (from > to)
            throw new ArgumentException("from > to");

        From = from;
        To = to;
    }

    public int From { get; }
    public int To { get; } // inclusive

    public override string ToString()
    {
        return From == To ? From.ToString() : $"{From}-{To}";
    }

    public bool Contains(int version)
    {
        return version >= From && version <= To;
    }
}