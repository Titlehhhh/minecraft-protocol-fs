namespace MinecraftDataFSharp
{
    public abstract class OpenSignEntity : IServerPacket
    {
        public Position Location { get; set; }

        public sealed class V340_762 : OpenSignEntity
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Location = reader.ReadPosition(protocolVersion);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 762;
            }
        }

        public sealed class V763_769 : OpenSignEntity
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Location = reader.ReadPosition(protocolVersion);
                IsFrontText = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 763 and <= 769;
            }

            public bool IsFrontText { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_762.SupportedVersion(protocolVersion) || V763_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}