namespace MinecraftDataFSharp
{
    public class ClientCommand
    {
        public int ActionId { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int actionId)
        {
            writer.WriteVarInt(actionId);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, ActionId);
        }
    }
}