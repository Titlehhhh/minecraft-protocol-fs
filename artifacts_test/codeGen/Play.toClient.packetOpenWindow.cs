public abstract partial class PacketOpenWindow
{
    public int WindowId { get; set; }
    public int InventoryType { get; set; }
    public V735_764Fields? V735_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V735_764Fields
    {
        public string WindowTitle { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag WindowTitle { get; set; }
    }

    private class Impl : PacketOpenWindow
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 764:
                {
                    var v735_764 = V735_764.GetValueOrDefault();
                    writer.WriteVarInt(WindowId);
                    writer.WriteVarInt(InventoryType);
                    writer.WriteString(v735_764.WindowTitle);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteVarInt(WindowId);
                    writer.WriteVarInt(InventoryType);
                    writer.WriteType<NbtTag>(v765_772.WindowTitle, protocolVersion);
                    break;
                }
            }
        }
    }
}