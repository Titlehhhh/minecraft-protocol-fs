namespace MinecraftDataFSharp
{
    public abstract class DeathCombatEvent : IServerPacket
    {
        public int PlayerId { get; set; }

        public sealed class V755_762 : DeathCombatEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                PlayerId = reader.ReadVarInt();
                EntityId = reader.ReadSignedInt();
                Message = reader.ReadString();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 762;
            }

            public int EntityId { get; set; }
            public string Message { get; set; }
        }

        public sealed class V763_764 : DeathCombatEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                PlayerId = reader.ReadVarInt();
                Message = reader.ReadString();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 763 and <= 764;
            }

            public string Message { get; set; }
        }

        public sealed class V765_769 : DeathCombatEvent
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                PlayerId = reader.ReadVarInt();
                Message = reader.ReadNbtTag(false);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }

            public NbtTag Message { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755_762.SupportedVersion(protocolVersion) || V763_764.SupportedVersion(protocolVersion) || V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}