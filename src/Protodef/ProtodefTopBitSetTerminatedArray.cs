using System.Text.Json.Serialization;

namespace Protodef;

public sealed class ProtodefTopBitSetTerminatedArray : ProtodefType
{
    [JsonPropertyName("type")] public ProtodefType Type { get; set; }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get { yield return new KeyValuePair<string?, ProtodefType>("type", Type); }
    }
    
    public override bool TryReplaceChild(string? key, ProtodefType oldChild, ProtodefType newChild)
    {
        base.TryReplaceChild(key, oldChild, newChild);
        if (Type == oldChild || key == "type")
        {
            Type = newChild;
            return true;
        }

        return false;
    }

    public override object Clone()
    {
        return new ProtodefTopBitSetTerminatedArray { Type = (ProtodefType)Type.Clone() };
    }
}