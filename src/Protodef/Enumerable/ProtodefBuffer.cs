using System.Text.Json;
using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef.Enumerable;



public sealed class ProtodefBuffer : ProtodefType
{
    
    [JsonPropertyName("countType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProtodefType? CountType { get; set; }

    [JsonPropertyName("count")]
    [JsonConverter(typeof(FlexibleCountConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Count { get; set; }

    [JsonPropertyName("rest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Rest { get; set; }

    public override string? GetNetType()
    {
        return "byte[]";
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            if (CountType is not null)
                yield return new KeyValuePair<string?, ProtodefType>("countType", CountType);
        }
    }

    public override object Clone()
    {
        var owner = new ProtodefBuffer
        {
            CountType = (ProtodefType?)CountType?.Clone(),
            Count = Count,
            Rest = Rest
        };
        if (owner.CountType is not null) owner.CountType.Parent = owner;
        return owner;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ProtodefBuffer other)
            return Equals(other);
        return false;
    }

    private bool Equals(ProtodefBuffer other)
    {
        return Equals(CountType, other.CountType) && Equals(Count, other.Count) && Rest == other.Rest;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CountType, Count, Rest);
    }
}