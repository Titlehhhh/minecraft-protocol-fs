public abstract partial class PacketShowDialog
{
    public NbtTag Dialog { get; set; }

    private class Impl : PacketShowDialog
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 771 and <= 772:
                {
                    var v771_772 = V771_772.GetValueOrDefault();
                    writer.WriteType<NbtTag>(Dialog, protocolVersion);
                    break;
                }
            }
        }
    }
}