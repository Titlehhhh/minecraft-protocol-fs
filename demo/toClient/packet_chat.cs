namespace MinecraftDataFSharp
{
    public abstract class Chat : IServerPacket
    {
        public string Message { get; set; }
        public sbyte Position { get; set; }

        public sealed class V340_710 : Chat
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Message = reader.ReadString();
                Position = reader.ReadSignedByte();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 710;
            }
        }

        public sealed class V734_758 : Chat
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Message = reader.ReadString();
                Position = reader.ReadSignedByte();
                Sender = reader.ReadUUID();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 734 and <= 758;
            }

            public Guid Sender { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_710.SupportedVersion(protocolVersion) || V734_758.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}