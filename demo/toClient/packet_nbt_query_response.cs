namespace MinecraftDataFSharp
{
    public abstract class NbtQueryResponse : IServerPacket
    {
        public int TransactionId { get; set; }
        public NbtTag? Nbt { get; set; }

        public sealed class V393_763 : NbtQueryResponse
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                TransactionId = reader.ReadVarInt();
                Nbt = reader.ReadOptionalNbt(true);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 763;
            }
        }

        public sealed class V764_769 : NbtQueryResponse
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                TransactionId = reader.ReadVarInt();
                Nbt = reader.ReadOptionalNbt(false);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 764 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393_763.SupportedVersion(protocolVersion) || V764_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}