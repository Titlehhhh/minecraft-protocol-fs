namespace MinecraftDataFSharp
{
    public abstract class DamageEvent : IServerPacket
    {
        public int EntityId { get; set; }
        public int SourceTypeId { get; set; }
        public int SourceCauseId { get; set; }
        public int SourceDirectId { get; set; }
        public Vector3F64? SourcePosition { get; set; }

        public sealed class V762_769 : DamageEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                EntityId = reader.ReadVarInt();
                SourceTypeId = reader.ReadVarInt();
                SourceCauseId = reader.ReadVarInt();
                SourceDirectId = reader.ReadVarInt();
                SourcePosition = reader.ReadOptional<Vector3F64?>(ReadDelegates.Vector364);
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