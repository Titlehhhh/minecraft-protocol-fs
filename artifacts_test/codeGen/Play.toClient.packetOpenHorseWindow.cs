public abstract partial class PacketOpenHorseWindow
{
    public int NbSlots { get; set; }
    public int EntityId { get; set; }
    public V735_765Fields? V735_765 { get; set; }
    public V766_772Fields? V766_772 { get; set; }

    public partial struct V735_765Fields
    {
        public byte WindowId { get; set; }
    }

    public partial struct V766_772Fields
    {
        public int WindowId { get; set; }
    }

    private class Impl : PacketOpenHorseWindow
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 765:
                {
                    var v735_765 = V735_765.GetValueOrDefault();
                    writer.WriteUnsignedByte(v735_765.WindowId);
                    writer.WriteVarInt(NbSlots);
                    writer.WriteSignedInt(EntityId);
                    break;
                }

                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteType<ContainerID>(v766_772.WindowId, protocolVersion);
                    writer.WriteVarInt(NbSlots);
                    writer.WriteSignedInt(EntityId);
                    break;
                }
            }
        }
    }
}