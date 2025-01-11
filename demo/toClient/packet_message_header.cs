namespace MinecraftDataFSharp
{
    public abstract class MessageHeader : IServerPacket
    {
        public byte[]? PreviousSignature { get; set; }
        public Guid SenderUuid { get; set; }
        public byte[] Signature { get; set; }
        public byte[] MessageHash { get; set; }

        public sealed class V760 : MessageHeader
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                PreviousSignature = reader.ReadOptional<byte[]?>(r_0 => r_0.ReadBuffer(LengthFormat.VarInt));
                SenderUuid = reader.ReadUUID();
                Signature = reader.ReadBuffer(LengthFormat.VarInt);
                MessageHash = reader.ReadBuffer(LengthFormat.VarInt);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion == 760;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V760.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}