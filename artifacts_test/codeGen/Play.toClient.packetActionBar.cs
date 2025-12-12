public abstract partial class PacketActionBar
{
    public V755_764Fields? V755_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V755_764Fields
    {
        public string Text { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag Text { get; set; }
    }

    private class Impl : PacketActionBar
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 764:
                {
                    var v755_764 = V755_764.GetValueOrDefault();
                    writer.WriteString(v755_764.Text);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteType<NbtTag>(v765_772.Text, protocolVersion);
                    break;
                }
            }
        }
    }
}