using System.Text.Json.Serialization;

namespace Protodef;

public sealed class ProtodefTopBitSetTerminatedArray : ProtodefType
{
    [JsonPropertyName("type")] public ProtodefType Type { get; set; }

    public override IEnumerable<KeyValuePair<string?, ProtodefType>> Children
    {
        get { yield return new KeyValuePair<string?, ProtodefType>("type", Type); }
    }

    public override object Clone()
    {
        return new ProtodefTopBitSetTerminatedArray { Type = (ProtodefType)Type.Clone() };
    }
}