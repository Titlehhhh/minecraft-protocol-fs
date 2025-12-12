public abstract partial class PacketRecipeBook
{
    public int BookId { get; set; }
    public bool BookOpen { get; set; }
    public bool FilterActive { get; set; }

    private class Impl : PacketRecipeBook
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 751 and <= 772:
                {
                    var v751_772 = V751_772.GetValueOrDefault();
                    writer.WriteVarInt(BookId);
                    writer.WriteBool(BookOpen);
                    writer.WriteBool(FilterActive);
                    break;
                }
            }
        }
    }
}