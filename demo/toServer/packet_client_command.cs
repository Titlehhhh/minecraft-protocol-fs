namespace MinecraftDataFSharp
{
    public sealed class ClientCommand
    {
        public int ActionId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, ActionId);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, int actionId)
        {
            writer.WriteVarInt(actionId);
        }
    }
}