namespace SandBoxLib;

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