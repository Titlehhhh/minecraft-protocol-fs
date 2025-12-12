public abstract partial class PacketEnchantItem
{
    public sbyte Enchantment { get; set; }
    public V735_766Fields? V735_766 { get; set; }
    public V767_772Fields? V767_772 { get; set; }

    public partial struct V735_766Fields
    {
        public sbyte WindowId { get; set; }
    }

    public partial struct V767_772Fields
    {
        public int WindowId { get; set; }
    }

    private class Impl : PacketEnchantItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 766:
                {
                    var v735_766 = V735_766.GetValueOrDefault();
                    writer.WriteSignedByte(v735_766.WindowId);
                    writer.WriteSignedByte(Enchantment);
                    break;
                }

                case >= 767 and <= 772:
                {
                    var v767_772 = V767_772.GetValueOrDefault();
                    writer.WriteType<ContainerID>(v767_772.WindowId, protocolVersion);
                    writer.WriteSignedByte(Enchantment);
                    break;
                }
            }
        }
    }
}