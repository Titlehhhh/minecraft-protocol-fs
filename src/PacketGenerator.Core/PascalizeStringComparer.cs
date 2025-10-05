using System.Collections;
using Humanizer;

namespace PacketGenerator.Core;

public class PascalizeStringComparer : IEqualityComparer<string>
{
    private static readonly PascalizeStringComparer instance = new PascalizeStringComparer();
    
    public static PascalizeStringComparer Instance => instance;
    
    public bool Equals(string? x, string? y)
    {
        if (x is null || y is null)
            return false;
        return x.Pascalize() == y.Pascalize();
    }

    public int GetHashCode(string obj)
    {
        return obj.Pascalize().GetHashCode();
    }
}