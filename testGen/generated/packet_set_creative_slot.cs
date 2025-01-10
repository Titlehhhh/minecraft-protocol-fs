namespace MinecraftDataFSharp
{
    public class SetCreativeSlot
    {
        public short Slot { get; set; }
        public Slot Item { get; set; }

        public new static bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 769;
        }

        internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, short slot, Slot item)
        {
            writer.WriteSignedShort(slot);
            writer.WriteSlot(item, protocolVersion);
        }

        public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            SerializeInternal(writer, protocolVersion, Slot, Item);
        }
    }
}