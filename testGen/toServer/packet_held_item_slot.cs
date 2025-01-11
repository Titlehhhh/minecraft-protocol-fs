namespace MinecraftDataFSharp
{
    public sealed class HeldItemSlot
    {
        public short SlotId { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, SlotId);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, short slotId)
        {
            writer.WriteSignedShort(slotId);
        }
    }
}