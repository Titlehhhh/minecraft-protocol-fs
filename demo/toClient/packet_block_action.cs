namespace MinecraftDataFSharp
{
    public sealed class BlockAction
    {
        public Position Location { get; set; }
        public byte Byte1 { get; set; }
        public byte Byte2 { get; set; }
        public int BlockId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            Location = reader.ReadPosition(protocolVersion);
            Byte1 = reader.ReadUnsignedByte();
            Byte2 = reader.ReadUnsignedByte();
            BlockId = reader.ReadVarInt();
        }
    }
}