namespace MinecraftDataFSharp
{
    public class TeleportConfirm
    {
        public int TeleportId { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int teleportId)
        {
            writer.WriteVarInt(teleportId);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, TeleportId);
        }
    }
}