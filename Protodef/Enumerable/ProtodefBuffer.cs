using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public sealed class ProtodefBuffer : ProtodefType
{
    [JsonPropertyName("countType")] public ProtodefType? CountType { get; set; }

    [JsonPropertyName("count")] public object? Count { get; set; }

    [JsonPropertyName("rest")] public bool? Rest { get; set; }

    public override string? GetNetType()
    {
        return "byte[]";
    }

    public override IEnumerator<KeyValuePair<string?, ProtodefType>> GetEnumerator()
    {
        if (CountType is not null)
            yield return new KeyValuePair<string?, ProtodefType>("countType", CountType);
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
}