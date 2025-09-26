using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public sealed class ProtodefBuffer : ProtodefType
{
    [JsonConstructor]
    public ProtodefBuffer(ProtodefType? countType, object? count, bool? rest)
    {
        CountType = countType;
        Count = count;
        Rest = rest;
    }

    public ProtodefBuffer()
    {
    }

    [JsonPropertyName("countType")] public ProtodefType? CountType { get; set; }

    [JsonPropertyName("count")] public object? Count { get; set; }

    [JsonPropertyName("rest")] public bool? Rest { get; set; }

    public override string? GetNetType()
    {
        return "byte[]";
    }

    public override IEnumerable<KeyValuePair<string?, ProtodefType>> Children
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
        if (obj is not ProtodefBuffer other)
        {
            return false;
        }

        return CountType == other.CountType && Count == other.Count && Rest == other.Rest;
    }
}