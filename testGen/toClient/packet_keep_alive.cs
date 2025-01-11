namespace MinecraftDataFSharp
{
    public sealed class KeepAlive
    {
        public long KeepAliveId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            KeepAliveId = reader.ReadSignedLong();
        }
    }
}