namespace MinecraftDataFSharp
{
    public abstract class SetCooldown : IServerPacket
    {
        public int CooldownTicks { get; set; }

        public sealed class V340_767 : SetCooldown
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ItemID = reader.ReadVarInt();
                CooldownTicks = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 767;
            }

            public int ItemID { get; set; }
        }

        public sealed class V768_769 : SetCooldown
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                CooldownGroup = reader.ReadString();
                CooldownTicks = reader.ReadVarInt();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }

            public string CooldownGroup { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_767.SupportedVersion(protocolVersion) || V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}