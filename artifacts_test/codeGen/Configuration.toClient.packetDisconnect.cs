public abstract partial class PacketDisconnect
{
    public V764Fields? V764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V764Fields
    {
        public string Reason { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag Reason { get; set; }
    }

    private class Impl : PacketDisconnect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 764:
                {
                    var v764 = V764.GetValueOrDefault();
                    writer.WriteString(v764.Reason);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteType<NbtTag>(v765_772.Reason, protocolVersion);
                    break;
                }
            }
        }
    }
}