namespace MinecraftDataFSharp
{
    public abstract class SetTitleSubtitle : IServerPacket
    {
        public sealed class V755_764 : SetTitleSubtitle
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Text = reader.ReadString();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 755 and <= 764;
            }

            public string Text { get; set; }
        }

        public sealed class V765_769 : SetTitleSubtitle
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                Text = reader.ReadNbtTag(false);
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 769;
            }

            public NbtTag Text { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V755_764.SupportedVersion(protocolVersion) || V765_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}