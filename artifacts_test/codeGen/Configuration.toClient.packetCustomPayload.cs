public abstract partial class PacketCustomPayload
{
    public string Channel { get; set; }
    public byte[] Data { get; set; }

    private class Impl : PacketCustomPayload
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteString(Channel);
                    writer.WriteRestBuffer(Data);
                    break;
                }
            }
        }
    }
}