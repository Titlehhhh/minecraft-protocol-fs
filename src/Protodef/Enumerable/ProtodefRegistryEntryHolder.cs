using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public class ProtodefRegistryEntryHolder : ProtodefType
{
    [JsonPropertyName("baseName")]
    public string BaseName { get; set; }
    
    [JsonPropertyName("otherwise")]
    public ProtodefContainerField Otherwise { get; set; } 
    public override object Clone()
    {
        var newObj = new ProtodefRegistryEntryHolder
        {
            BaseName = BaseName,
            Otherwise = (ProtodefContainerField)Otherwise.Clone()
        };
        return newObj;
    }
    
    private bool Equals(ProtodefRegistryEntryHolder other)
    {
        return BaseName == other.BaseName && Otherwise.Equals(other.Otherwise);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProtodefRegistryEntryHolder other && Equals(other));
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("otherwise", Otherwise.Type);
        }
    }
}