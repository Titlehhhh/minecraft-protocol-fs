using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SandBoxLib;
public delegate T ReadDelegate<out T>(IMinecraftPrimitiveReader reader);
public delegate I LengthDelegate<I>(IMinecraftPrimitiveReader reader) where I : System.Numerics.IBinaryInteger<I>;

public static class ReadDelegates
{
    public static readonly ReadDelegate<byte> Byte = reader => reader.ReadUnsignedByte();
    public static readonly ReadDelegate<sbyte> SByte = reader => reader.ReadSignedByte();

    public static readonly ReadDelegate<int> VarInt = reader => reader.ReadVarInt();
    public static readonly ReadDelegate<int> Int32 = reader => reader.ReadSignedInt();
    public static readonly ReadDelegate<uint> UInt32 = reader => reader.ReadUnsignedInt();
    public static readonly ReadDelegate<long> Int64 = reader => reader.ReadSignedLong();
    public static readonly ReadDelegate<ulong> UInt64 = reader => reader.ReadUnsignedLong();

    public static readonly ReadDelegate<short> Int16 = reader => reader.ReadSignedShort();
    public static readonly ReadDelegate<ushort> UInt16 = reader => reader.ReadUnsignedShort();
    public static readonly ReadDelegate<float> Float = reader => reader.ReadFloat();
    public static readonly ReadDelegate<double> Double = reader => reader.ReadDouble();

}
public static class LengthDelegates
{

    public static readonly LengthDelegate<int> VarIntLength = s => s.ReadVarInt();
    public static readonly LengthDelegate<int> IntLength = s => s.ReadSignedInt();
    public static readonly LengthDelegate<short> ShortLength = s => s.ReadSignedShort();
    public static readonly LengthDelegate<byte> ByteLength = s => s.ReadUnsignedByte();
}

public interface IMinecraftPrimitiveWriter : IDisposable
{
    void WriteBoolean(bool value);
    void WriteSignedByte(sbyte value);
    void WriteUnsignedByte(byte value);
    void WriteUnsignedShort(ushort value);
    void WriteSignedShort(short value);
    void WriteSignedInt(int value);
    void WriteUnsignedInt(uint value);
    void WriteSignedLong(long value);
    void WriteUnsignedLong(ulong value);
    void WriteFloat(float value);
    void WriteDouble(double value);
    void WriteUUID(Guid value);
    void WriteBuffer(ReadOnlySpan<byte> value);
    void WriteVarInt(int? value);
    void WriteVarInt(int value);
    void WriteVarLong(long value);
    void WriteString(string value);
    void WriteOptionalNbt(NbtTag? value);
    void WriteNbt(NbtTag value);
    MemoryOwner<byte> GetWrittenMemory();
}

public interface IMinecraftPrimitiveReader
{
    int RemainingCount { get; }
    int ReadVarInt();
    int Read(Span<byte> output);
    ReadOnlySpan<byte> Read(int count);
    long ReadVarLong();
    bool ReadBoolean();
    byte ReadUnsignedByte();
    sbyte ReadSignedByte();
    ushort ReadUnsignedShort();
    short ReadSignedShort();
    int ReadSignedInt();
    uint ReadUnsignedInt();
    long ReadSignedLong();
    ulong ReadUnsignedLong();
    float ReadFloat();
    double ReadDouble();
    string ReadString();
    Guid ReadUUID();
    byte[] ReadRestBuffer();
    byte[] ReadBuffer(int length);
    NbtTag? ReadOptionalNbt(bool readRootTag);
    NbtTag ReadNbt(bool readRootTag);

}

public sealed class MinecraftPrimitiveReader : IMinecraftPrimitiveReader
{
    public int Read(Span<byte> output)
    {
        throw new NotImplementedException();
    }

    public bool ReadBoolean()
    {
        throw new NotImplementedException();
    }

    public byte[] ReadBuffer(int length)
    {
        throw new NotImplementedException();
    }

    public double ReadDouble()
    {
        throw new NotImplementedException();
    }

    public float ReadFloat()
    {
        throw new NotImplementedException();
    }

    public NbtTag ReadNbt(bool readRootTag)
    {
        throw new NotImplementedException();
    }

    public NbtTag? ReadOptionalNbt(bool readRootTag)
    {
        throw new NotImplementedException();
    }

    public byte[] ReadRestBuffer()
    {
        throw new NotImplementedException();
    }

    public sbyte ReadSignedByte()
    {
        throw new NotImplementedException();
    }

    public int ReadSignedInt()
    {
        throw new NotImplementedException();
    }

    public long ReadSignedLong()
    {
        throw new NotImplementedException();
    }

    public short ReadSignedShort()
    {
        throw new NotImplementedException();
    }

    public string ReadString()
    {

        throw new NotImplementedException();
    }

    public byte ReadUnsignedByte()
    {
        throw new NotImplementedException();
    }

    public uint ReadUnsignedInt()
    {
        throw new NotImplementedException();
    }

    public ulong ReadUnsignedLong()
    {
        throw new NotImplementedException();
    }

    public ushort ReadUnsignedShort()
    {
        throw new NotImplementedException();
    }

    public Guid ReadUUID()
    {
        throw new NotImplementedException();
    }

    public int ReadVarInt()
    {
        throw new NotImplementedException();
    }

    public long ReadVarLong()
    {
        throw new NotImplementedException();
    }






    public int RemainingCount => throw new NotImplementedException();






    public ReadOnlySpan<byte> Read(int count)
    {
        throw new NotImplementedException();
    }
}

public static class ReadExtensions
{
    public static byte[] ReadBuffer<I>(this IMinecraftPrimitiveReader reader, LengthDelegate<I> length) where I : IBinaryInteger<I>
    {
        int len = int.CreateChecked(length(reader));
        return reader.ReadBuffer(len);
    }

    public static T[] ReadArray<T, I>(this IMinecraftPrimitiveReader reader, LengthDelegate<I> length, ReadDelegate<T> readDelegate)
        where I : System.Numerics.IBinaryInteger<I>
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
