public abstract partial class PacketHeldItemSlot
{
    public V735_768Fields? V735_768 { get; set; }
    public V769_772Fields? V769_772 { get; set; }

    public partial struct V735_768Fields
    {
        public sbyte Slot { get; set; }
    }

    public partial struct V769_772Fields
    {
        public int Slot { get; set; }
    }

    private class Impl : PacketHeldItemSlot
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 768:
                {
                    var v735_768 = V735_768.GetValueOrDefault();
                    writer.WriteSignedByte(v735_768.Slot);
                    break;
                }

                case >= 769 and <= 772:
                {
                    var v769_772 = V769_772.GetValueOrDefault();
                    writer.WriteVarInt(v769_772.Slot);
                    break;
                }
            }
        }
    }
}