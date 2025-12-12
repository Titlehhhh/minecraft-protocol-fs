public abstract partial class PacketSetTitleTime
{
    public int FadeIn { get; set; }
    public int Stay { get; set; }
    public int FadeOut { get; set; }

    private class Impl : PacketSetTitleTime
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteSignedInt(FadeIn);
                    writer.WriteSignedInt(Stay);
                    writer.WriteSignedInt(FadeOut);
                    break;
                }
            }
        }
    }
}