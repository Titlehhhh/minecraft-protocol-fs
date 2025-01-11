namespace MinecraftDataFSharp
{
    public sealed class BlockBreakAnimation
    {
        public int EntityId { get; set; }
        public Position Location { get; set; }
        public sbyte DestroyStage { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadVarInt();
            Location = reader.ReadPosition(protocolVersion);
            DestroyStage = reader.ReadSignedByte();
        }
    }
}