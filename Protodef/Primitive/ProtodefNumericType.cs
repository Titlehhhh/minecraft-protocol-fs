namespace Protodef.Primitive;

public sealed class ProtodefNumericType : ProtodefType
{
    public ProtodefNumericType(string name, string protodefName, bool signed, ByteOrder order)
    {
        NetName = name;
        ProtodefName = protodefName;
        Signed = signed;
        Order = order;
    }

    public string NetName { get; }

    public string ProtodefName { get; }

    public bool Signed { get; }

    public ByteOrder Order { get; }

    public override string ToString()
    {
        return ProtodefName;
    }

    public override string? GetNetType()
    {
        return NetName;
    }

    public override object Clone()
    {
        return new ProtodefNumericType(NetName, ProtodefName, Signed, Order);
    }

    private bool Equals(ProtodefNumericType other)
    {
        return NetName == other.NetName && ProtodefName == other.ProtodefName && Signed == other.Signed &&
               Order == other.Order;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProtodefNumericType other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NetName, ProtodefName, Signed, (int)Order);
    }
}
