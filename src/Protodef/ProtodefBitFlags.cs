namespace Protodef;


public sealed class ProtodefBitFlags : ProtodefType
{
    
    public string Type { get; }

    public object[] Flags { get; }

    public bool Big { get; }

    public int Shift { get; }

    public ProtodefBitFlags(string type, object[] flags, bool big = false, int shift = 0)
    {
        Type = type;
        Flags = flags;
        Big = big;
        Shift = shift;
    }

    public override object Clone()
    {
        return new ProtodefBitFlags(Type, Flags, Big, Shift);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ProtodefBitFlags other)
        {
            return false;
        }

        return Type == other.Type && Flags.SequenceEqual(other.Flags) && Big == other.Big && Shift == other.Shift;
    }
}