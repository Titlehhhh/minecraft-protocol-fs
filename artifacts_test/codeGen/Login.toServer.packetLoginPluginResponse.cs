public abstract partial class PacketLoginPluginResponse
{
    public int MessageId { get; set; }
    public byte[]? Data { get; set; }

    private class Impl : PacketLoginPluginResponse
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(MessageId);
                    writer.WriteOptional(Data, protocolVersion, static writer =>
                    {
                        writer.WriteRestBuffer(writer);
                    });
                    break;
                }
            }
        }
    }
}