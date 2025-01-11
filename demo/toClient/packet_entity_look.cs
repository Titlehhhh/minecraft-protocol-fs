namespace MinecraftDataFSharp
{
    public sealed class EntityLook
    {
        public int EntityId { get; set; }
        public sbyte Yaw { get; set; }
        public sbyte Pitch { get; set; }
        public bool OnGround { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            EntityId = reader.ReadVarInt();
            Yaw = reader.ReadSignedByte();
            Pitch = reader.ReadSignedByte();
            OnGround = reader.ReadBoolean();
        }
    }
}