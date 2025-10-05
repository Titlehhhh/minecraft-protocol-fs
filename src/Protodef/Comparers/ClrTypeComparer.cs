namespace Protodef.Comparers;

public class ClrTypeComparer : IEqualityComparer<ProtodefType>
{
    private static readonly ClrTypeComparer instance = new ClrTypeComparer();
    
    public static ClrTypeComparer Instance => instance;
    public bool Equals(ProtodefType? x, ProtodefType? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;

        return (x.GetClrType(), y.GetClrType()) switch
        {
            ({ } p1, { } p2) => p1 == p2 && !string.IsNullOrWhiteSpace(p1) && !string.IsNullOrWhiteSpace(p2),
            _ => x.Equals(y)
        };
    }

    public int GetHashCode(ProtodefType obj)
    {
        if (obj.GetClrType() is { } str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                return str.GetHashCode();
            }
        }
        
        return obj.GetHashCode();
    }
}