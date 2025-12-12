public abstract partial class PacketSetSlot
{
    public short Slot { get; set; }
    public V735_755Fields? V735_755 { get; set; }
    public V756_765Fields? V756_765 { get; set; }
    public V766_772Fields? V766_772 { get; set; }

    public partial struct V735_755Fields
    {
        public sbyte WindowId { get; set; }
        public Slot Item { get; set; }
    }

    public partial struct V756_765Fields
    {
        public sbyte WindowId { get; set; }
        public int StateId { get; set; }
        public Slot Item { get; set; }
    }

    public partial struct V766_772Fields
    {
        public int WindowId { get; set; }
        public int StateId { get; set; }
        public Slot Item { get; set; }
    }

    private class Impl : PacketSetSlot
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 755:
                {
                    var v735_755 = V735_755.GetValueOrDefault();
                    writer.WriteSignedByte(v735_755.WindowId);
                    writer.WriteSignedShort(Slot);
                    writer.WriteType<Slot>(v735_755.Item, protocolVersion);
                    break;
                }

                case >= 756 and <= 765:
                {
                    var v756_765 = V756_765.GetValueOrDefault();
                    writer.WriteSignedByte(v756_765.WindowId);
                    writer.WriteVarInt(v756_765.StateId);
                    writer.WriteSignedShort(Slot);
                    writer.WriteType<Slot>(v756_765.Item, protocolVersion);
                    break;
                }

                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteType<ContainerID>(v766_772.WindowId, protocolVersion);
                    writer.WriteVarInt(v766_772.StateId);
                    writer.WriteSignedShort(Slot);
                    writer.WriteType<Slot>(v766_772.Item, protocolVersion);
                    break;
                }
            }
        }
    }
}