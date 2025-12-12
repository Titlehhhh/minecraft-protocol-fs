public abstract partial class PacketSelectBundleItem
{
    public int SlotId { get; set; }
    public int SelectedItemIndex { get; set; }

    private class Impl : PacketSelectBundleItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(SlotId);
                    writer.WriteVarInt(SelectedItemIndex);
                    break;
                }
            }
        }
    }
}