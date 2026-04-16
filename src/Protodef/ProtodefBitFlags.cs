using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef;

[JsonConverter(typeof(ProtodefBitFlagsConverter))]
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
        unchecked
        {
            int hash = Type?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ (Flags?.Length.GetHashCode() ?? 0);
            if (Flags is not null)
            {
                foreach (var flag in Flags)
                {
                    hash = (hash * 397) ^ (flag?.GetHashCode() ?? 0);
                }
            }
            hash = (hash * 397) ^ Big.GetHashCode();
            hash = (hash * 397) ^ Shift.GetHashCode();
            return hash;
        }
    }
}