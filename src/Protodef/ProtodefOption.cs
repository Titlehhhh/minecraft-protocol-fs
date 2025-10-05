namespace Protodef;

public sealed class ProtodefOption : ProtodefType
{
    public ProtodefOption(ProtodefType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }

    public ProtodefType Type { get; set; }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            yield return new KeyValuePair<string?, ProtodefType>("type", Type);
        }
    }


    public override object Clone()
    {
        var owner = new ProtodefOption((ProtodefType)Type.Clone());
        owner.Type.Parent = owner;
        return owner;
    }

    public override string? GetClrType()
    {
        var netType = Type.GetClrType();
        if (netType is not null) return netType + "?";
        return null;
    }



    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ProtodefOption)obj);
    }

    private bool Equals(ProtodefOption other)
    {
        return Type.Equals(other.Type);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type.GetHashCode());
    }
}

