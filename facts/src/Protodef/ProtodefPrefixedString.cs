using System.Text.Json.Serialization;

namespace Protodef;

public sealed class ProtodefPrefixedString : ProtodefType
{
    [JsonConstructor]
    public ProtodefPrefixedString(ProtodefType countType)
    {
        ArgumentNullException.ThrowIfNull(countType);
        CountType = countType;
    }

    [JsonPropertyName("countType")] public ProtodefType CountType { get; set; }

    public override string? GetClrType()
    {
        return "string";
    }

    public override object Clone()
    {
        var owner = new ProtodefPrefixedString((ProtodefType)CountType.Clone());
        owner.CountType.Parent = owner;
        return owner;
    }

    private bool Equals(ProtodefPrefixedString other)
    {
        return CountType.Equals(other.CountType);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProtodefPrefixedString other && Equals(other));
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("countType", CountType);
        }
    }
    
    public override bool TryReplaceChild(string? key, ProtodefType oldChild, ProtodefType newChild)
    {
        base.TryReplaceChild(key, oldChild, newChild);
        if (CountType == oldChild || key == "countType")
        {
            CountType = newChild;
            return true;
        }

        return false;
    }


    public override int GetHashCode()
    {
        return CountType.GetHashCode();
    }
    public override string ToString() => "pstring";
}
