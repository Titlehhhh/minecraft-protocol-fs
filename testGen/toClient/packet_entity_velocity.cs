namespace MinecraftDataFSharp
{
    public sealed class EntityVelocity
    {
        public int EntityId { get; set; }
        public short VelocityX { get; set; }
        public short VelocityY { get; set; }
        public short VelocityZ { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadVarInt();
            VelocityX = reader.ReadSignedShort();
            VelocityY = reader.ReadSignedShort();
            VelocityZ = reader.ReadSignedShort();
        }
    }
}