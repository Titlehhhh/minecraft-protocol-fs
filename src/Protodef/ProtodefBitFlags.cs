using System.Text.Json.Serialization;

namespace Protodef;


public sealed class ProtodefBitFlags : ProtodefType
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("flags")]
    public object[] Flags { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Big { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Shift { get; set; }

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

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Flags, Big, Shift);
    }
}