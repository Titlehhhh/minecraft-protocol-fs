public abstract partial class PacketCommonTransfer
{
    public string Host { get; set; }
    public int Port { get; set; }

    private class Impl : PacketCommonTransfer
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteString(Host);
                    writer.WriteVarInt(Port);
                    break;
                }
            }
        }
    }
}