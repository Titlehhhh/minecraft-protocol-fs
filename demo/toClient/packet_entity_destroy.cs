namespace MinecraftDataFSharp
{
    public abstract class EntityDestroy : IServerPacket
    {
        public int[] EntityIds { get; set; }

        public sealed class V340_754 : EntityDestroy
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityIds = reader.ReadArray<int, int>(LengthFormat.VarInt, ReadDelegates.VarInt);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 754;
            }
        }

        public sealed class V756_769 : EntityDestroy
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityIds = reader.ReadArray<int, int>(LengthFormat.VarInt, ReadDelegates.VarInt);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 756 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_754.SupportedVersion(protocolVersion) || V756_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}