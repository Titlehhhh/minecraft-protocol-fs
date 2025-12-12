public abstract partial class PacketHeldItemSlot
{
    public short SlotId { get; set; }

    private class Impl : PacketHeldItemSlot
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedShort(SlotId);
                    break;
                }
            }
        }
    }
}