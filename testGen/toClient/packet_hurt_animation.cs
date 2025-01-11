namespace MinecraftDataFSharp
{
    public abstract class HurtAnimation : IServerPacket
    {
        public int EntityId { get; set; }
        public float Yaw { get; set; }

        public sealed class V762_769 : HurtAnimation
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
                Yaw = reader.ReadFloat();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 762 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V762_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}