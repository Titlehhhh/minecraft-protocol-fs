public abstract partial class PacketKeepAlive
{
    public long KeepAliveId { get; set; }

    private class Impl : PacketKeepAlive
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteSignedLong(KeepAliveId);
                    break;
                }
            }
        }
    }
}