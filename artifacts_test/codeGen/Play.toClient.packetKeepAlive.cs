public abstract partial class PacketKeepAlive
{
    public long KeepAliveId { get; set; }

    private class Impl : PacketKeepAlive
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedLong(KeepAliveId);
                    break;
                }
            }
        }
    }
}