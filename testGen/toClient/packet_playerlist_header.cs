namespace MinecraftDataFSharp
{
    public abstract class PlayerlistHeader : IServerPacket
    {
        public sealed class V340_764 : PlayerlistHeader
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Header = reader.ReadString();
                Footer = reader.ReadString();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 764;
            }

            public string Header { get; set; }
            public string Footer { get; set; }
        }

        public sealed class V765_769 : PlayerlistHeader
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Header = reader.ReadNbtTag(false);
                Footer = reader.ReadNbtTag(false);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }

            public NbtTag Header { get; set; }
            public NbtTag Footer { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_764.SupportedVersion(protocolVersion) || V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}