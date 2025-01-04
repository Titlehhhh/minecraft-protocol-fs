namespace SandBoxLib;

public static class LengthDelegates
{

    public static readonly LengthDelegate<int> VarIntLength = s => s.ReadVarInt();
    public static readonly LengthDelegate<int> IntLength = s => s.ReadSignedInt();
    public static readonly LengthDelegate<short> ShortLength = s => s.ReadSignedShort();
    public static readonly LengthDelegate<byte> ByteLength = s => s.ReadUnsignedByte();
}