public abstract partial class PacketServerInfo
{
    public string Response { get; set; }

    private class Impl : PacketServerInfo
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteString(Response);
                    break;
                }
            }
        }
    }
}