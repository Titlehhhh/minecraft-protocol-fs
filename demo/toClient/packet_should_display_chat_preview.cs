namespace MinecraftDataFSharp
{
    public abstract class ShouldDisplayChatPreview : IServerPacket
    {
        public bool ShouldDisplayChatPreview { get; set; }

        public sealed class V759_760 : ShouldDisplayChatPreview
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                ShouldDisplayChatPreview = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 760;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V759_760.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}