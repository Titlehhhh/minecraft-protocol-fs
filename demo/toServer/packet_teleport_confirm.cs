namespace MinecraftDataFSharp
{
    public sealed class TeleportConfirm
    {
        public int TeleportId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, TeleportId);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int teleportId)
        {
            writer.WriteVarInt(teleportId);
        }
    }
}