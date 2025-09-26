using System.Text.Json.Serialization;

namespace Protodef;

public sealed class ProtodefLoop : ProtodefType
{
    private ProtodefType _type;
    [JsonPropertyName("endVal")] public uint EndValue { get; set; }

    [JsonPropertyName("type")]
    public ProtodefType Type
    {
        get => _type;
        set => _type = value ?? throw new ArgumentNullException("value");
    }

    public override IEnumerable<KeyValuePair<string?, ProtodefType>> Children
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("type", Type);
        }
    }

    public override object Clone()
    {
        var owner = new ProtodefLoop
        {
            EndValue = EndValue,
            Type = (ProtodefType)Type.Clone()
        };
        owner.Type.Parent = owner;
        return owner;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ProtodefLoop other)
        {
            return false;
        }

        return EndValue == other.EndValue && Type.Equals(other.Type);
    }
}