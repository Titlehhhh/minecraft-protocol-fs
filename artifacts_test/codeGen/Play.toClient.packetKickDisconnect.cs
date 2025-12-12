public abstract partial class PacketKickDisconnect
{
    public V735_764Fields? V735_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V735_764Fields
    {
        public string Reason { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag Reason { get; set; }
    }

    private class Impl : PacketKickDisconnect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 764:
                {
                    var v735_764 = V735_764.GetValueOrDefault();
                    writer.WriteString(v735_764.Reason);
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