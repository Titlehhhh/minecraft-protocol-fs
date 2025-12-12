public abstract partial class PacketPingResponse
{
    public long Id { get; set; }

    private class Impl : PacketPingResponse
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteSignedLong(Id);
                    break;
                }
            }
        }
    }
}