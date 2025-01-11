namespace MinecraftDataFSharp
{
    public abstract class RemoveEntityEffect : IServerPacket
    {
        public int EntityId { get; set; }

        public sealed class V340_757 : RemoveEntityEffect
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
                EffectId = reader.ReadSignedByte();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 757;
            }

            public sbyte EffectId { get; set; }
        }

        public sealed class V758_769 : RemoveEntityEffect
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
                EffectId = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 758 and <= 769;
            }

            public int EffectId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_757.SupportedVersion(protocolVersion) || V758_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}