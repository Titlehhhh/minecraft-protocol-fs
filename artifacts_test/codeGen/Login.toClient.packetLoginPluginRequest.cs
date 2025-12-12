public abstract partial class PacketLoginPluginRequest
{
    public int MessageId { get; set; }
    public string Channel { get; set; }
    public byte[] Data { get; set; }

    private class Impl : PacketLoginPluginRequest
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(MessageId);
                    writer.WriteString(Channel);
                    writer.WriteRestBuffer(Data);
                    break;
                }
            }
        }
    }
}