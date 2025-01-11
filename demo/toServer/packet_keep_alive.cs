namespace MinecraftDataFSharp
{
    public sealed class KeepAlive
    {
        public long KeepAliveId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, KeepAliveId);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, long keepAliveId)
        {
            writer.WriteSignedLong(keepAliveId);
        }
    }
}