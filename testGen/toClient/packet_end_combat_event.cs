namespace MinecraftDataFSharp
{
    public abstract class EndCombatEvent : IServerPacket
    {
        public int Duration { get; set; }

        public sealed class V755_762 : EndCombatEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Duration = reader.ReadVarInt();
                EntityId = reader.ReadSignedInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 762;
            }

            public int EntityId { get; set; }
        }

        public sealed class V763_769 : EndCombatEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Duration = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 763 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755_762.SupportedVersion(protocolVersion) || V763_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}