namespace MinecraftDataFSharp
{
    public abstract class PlayerRotation : IServerPacket
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public sealed class V768_769 : PlayerRotation
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Yaw = reader.ReadFloat();
                Pitch = reader.ReadFloat();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}