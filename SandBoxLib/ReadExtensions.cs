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

        if (ReferenceEquals(readDelegate, ReadDelegates.Byte))
        {
            return (T[])(object)reader.ReadBuffer(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.SByte))
        {
            ReadOnlySpan<byte> buff = reader.Read(len);
            ReadOnlySpan<sbyte> casted = MemoryMarshal.Cast<byte, sbyte>(buff);
            return (T[])(object)casted.ToArray();
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.Int16))
        {
            return (T[])(object)reader.ReadArrayInt16BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.UInt16))
        {
            return (T[])(object)reader.ReadArrayUnsignedInt16BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.Int32))
        {
            return (T[])(object)reader.ReadArrayInt32BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.UInt32))
        {
            return (T[])(object)reader.ReadArrayUnsignedInt32BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.Int64))
        {
            return (T[])(object)reader.ReadArrayInt64BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.UInt64))
        {
            return (T[])(object)reader.ReadArrayUnsignedInt64BigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.Float))
        {
            return (T[])(object)reader.ReadArrayFloatBigEndian(len);
        }
        else if (ReferenceEquals(readDelegate, ReadDelegates.Double))
        {
            return (T[])(object)reader.ReadArrayDoubleBigEndian(len);
        }

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

    public static int[] ReadArrayInt32BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static short[] ReadArrayInt16BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static ushort[] ReadArrayUnsignedInt16BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static uint[] ReadArrayUnsignedInt32BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static ulong[] ReadArrayUnsignedInt64BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static long[] ReadArrayInt64BigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
    public static float[] ReadArrayFloatBigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        if (reader.RemainingCount < length)
        {
            throw new InsufficientMemoryException();
        }

        ReadOnlySpan<byte> bytes = reader.Read(sizeof(int) * length);
        if (BitConverter.IsLittleEndian)
        {
            ReadOnlySpan<int> ints = MemoryMarshal.Cast<byte, int>(bytes);
            float[] result = new float[length];
            BinaryPrimitives.ReverseEndianness(ints, MemoryMarshal.Cast<float, int>(result));
            return result;
        }
        return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
    }
    public static double[] ReadArrayDoubleBigEndian(this IMinecraftPrimitiveReader reader, int length)
    {
        throw null;
    }
}