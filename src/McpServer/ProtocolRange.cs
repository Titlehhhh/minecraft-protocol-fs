using System;

namespace McpServer;

public readonly record struct ProtocolRange
{
    public int From { get; }
    public int To { get; } // inclusive

    public ProtocolRange(int from, int to)
    {
        if (from > to)
            throw new ArgumentException("from > to");

        From = from;
        To = to;
    }

    public override string ToString()
    {
        return From == To ? From.ToString() : $"{From}-{To}";
    }

    public bool Contains(int version)
        => version >= From && version <= To;
}