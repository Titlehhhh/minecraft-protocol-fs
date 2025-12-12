public abstract partial class PacketPing
{
    public long Time { get; set; }

    private class Impl : PacketPing
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedLong(Time);
                    break;
                }
            }
        }
    }
}