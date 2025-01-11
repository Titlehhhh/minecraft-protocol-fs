namespace MinecraftDataFSharp
{
    public sealed class SetCreativeSlot
    {
        public short Slot { get; set; }
        public Slot Item { get; set; }

        public static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        public override void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(ref writer, protocolVersion, Slot, Item);
        }

        internal static void SerializeInternal(ref MinecraftPrimitiveWriter writer, int protocolVersion, short slot, Slot item)
        {
            writer.WriteSignedShort(slot);
            writer.WriteSlot(item, protocolVersion);
        }
    }
}