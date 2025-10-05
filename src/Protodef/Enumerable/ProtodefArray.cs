using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef.Enumerable;

public sealed class ProtodefArray : ProtodefType
{
    [JsonPropertyName("type")] public ProtodefType Type { get; set; }

    [JsonPropertyName("countType")] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProtodefType? CountType { get; set; }


    [JsonPropertyName("count")] 
    [JsonConverter(typeof(FlexibleCountConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Count { get; set; }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("type", Type);
            if (CountType is not null)
                yield return new KeyValuePair<string?, ProtodefType>("countType", CountType);
        }
    }

    public override string? GetClrType()
    {
        var netType = Type.GetClrType();
        if (netType is not null) return netType + "[]";
        return null;
    }

    public override object Clone()
    {
        var owner = new ProtodefArray
        {
            Type = (ProtodefType)Type.Clone(),
            CountType = CountType?.Clone() as ProtodefType,
            Count = Count
        };

        if (owner.CountType is not null) owner.CountType.Parent = owner;
        owner.Type.Parent = owner;
        return owner;
    }

    private bool Equals(ProtodefArray other)
    {
        return Type.Equals(other.Type) && Equals(CountType, other.CountType) && Equals(Count, other.Count);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProtodefArray other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, CountType, Count);
    }
}