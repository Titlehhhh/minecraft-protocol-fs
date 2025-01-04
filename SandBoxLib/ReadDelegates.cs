namespace SandBoxLib;

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