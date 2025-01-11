namespace MinecraftDataFSharp
{
    public sealed class Animation
    {
        public int EntityId { get; set; }
        public byte Animation { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadVarInt();
            Animation = reader.ReadUnsignedByte();
        }
    }
}