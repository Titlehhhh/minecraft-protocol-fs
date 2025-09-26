namespace Protodef.Primitive;

public sealed class ProtodefVoid : ProtodefType
{
    private static readonly int hash = "void".GetHashCode();

    public override string ToString()
    {
        return "void";
    }

    public override string? GetNetType()
    {
        return "void";
    }

    public override object Clone()
    {
        return new ProtodefVoid();
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ProtodefVoid;
    }

    public override int GetHashCode()
    {
        return hash;
    }
}