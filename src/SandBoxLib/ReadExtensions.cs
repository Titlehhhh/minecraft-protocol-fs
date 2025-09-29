using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SandBoxLib;

public static class ReadExtensions
{
    public static byte[] ReadBuffer<I>(this IMinecraftPrimitiveReader reader, LengthDelegate<I> length) where I : IBinaryInteger<I>
    {
        int len = int.CreateChecked(length(reader));
        return reader.ReadBuffer(len);
    }

    public static T[] ReadArray<T, TInteger>(this IMinecraftPrimitiveReader reader, LengthDelegate<TInteger> length, ReadDelegate<T> readDelegate)
        where TInteger : IBinaryInteger<TInteger>
    {
        int len = int.CreateChecked(length(reader));
        if (len == 0)
            return Array.Empty<T>();

        

        T[] arr = new T[len];
        for (int i = 0; i < len; i++)
        {
            arr[i] = readDelegate(reader);
        }
        return arr;
    }

    public static T? ReadOptional<T>(this IMinecraftPrimitiveReader reader, ReadDelegate<T> readDelegate)
    {
        if (reader.ReadBoolean())
            return readDelegate(reader);
        return default(T);
    }

    
    
}