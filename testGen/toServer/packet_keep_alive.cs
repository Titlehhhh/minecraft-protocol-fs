namespace MinecraftDataFSharp
{
    public class KeepAlive
    {
        public long KeepAliveId { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, long keepAliveId)
        {
            writer.WriteSignedLong(keepAliveId);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, KeepAliveId);
        }
    }
}