using System.Runtime.CompilerServices;

namespace SandBoxLib;
public delegate T ReadDelegate<out T>(IMinecraftPrimitiveReader reader);
public delegate I LengthDelegate<I>(IMinecraftPrimitiveReader reader) where I : System.Numerics.IBinaryInteger<I>;

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