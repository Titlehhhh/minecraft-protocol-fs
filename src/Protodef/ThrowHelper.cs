using System.Diagnostics.CodeAnalysis;

namespace Protodef;

static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowKeyNotFoundException(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        throw new KeyNotFoundException($"Key {key} was not found.");
    }
}