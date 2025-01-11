namespace MinecraftDataFSharp
{
    public abstract class UpdateTime : IServerPacket
    {
        public long Age { get; set; }
        public long Time { get; set; }

        public sealed class V340_767 : UpdateTime
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Age = reader.ReadSignedLong();
                Time = reader.ReadSignedLong();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }
        }

        public sealed class V768_769 : UpdateTime
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Age = reader.ReadSignedLong();
                Time = reader.ReadSignedLong();
                TickDayTime = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }

            public bool TickDayTime { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion) || V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}