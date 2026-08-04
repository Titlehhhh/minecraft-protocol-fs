using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public class ProtodefRegistryEntryHolderSet : ProtodefType
{
    [JsonPropertyName("base")]
    public ProtodefContainerField Base { get; set; }
    
    [JsonPropertyName("otherwise")]
    public ProtodefContainerField Otherwise { get; set; } 
    
    public override object Clone()
    {
        return new ProtodefRegistryEntryHolderSet
        {
            Base = (ProtodefContainerField)Base.Clone(),
            Otherwise = (ProtodefContainerField)Otherwise.Clone()
        };
    }
    
    private bool Equals(ProtodefRegistryEntryHolderSet other)
    {
        return Base.Equals(other.Base) && Otherwise.Equals(other.Otherwise);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProtodefRegistryEntryHolderSet other && Equals(other));
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("base", Base.Type);
            yield return new KeyValuePair<string?, ProtodefType>("otherwise", Otherwise.Type);
        }
    }

    public override bool TryReplaceChild(string? key, ProtodefType oldChild, ProtodefType newChild)
    {
        base.TryReplaceChild(key, oldChild, newChild);
        if (Base.Type == oldChild || key == "base")
        {
            Base.Type = newChild;
            return true;
        }

        if (Otherwise.Type == oldChild || key == "otherwise")
        {
            Otherwise.Type = newChild;
            return true;
        }

        return false;
    }
}