namespace MinecraftDataFSharp
{
    public abstract class PlayerRemove : IServerPacket
    {
        public Guid[] Players { get; set; }

        public sealed class V761_769 : PlayerRemove
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Players = reader.ReadArray<Guid, int>(LengthFormat.VarInt, r_0 => r_0.ReadUUID());
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 761 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V761_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}