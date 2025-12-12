public abstract partial class PacketSetProtocol
{
    public int ProtocolVersion { get; set; }
    public string ServerHost { get; set; }
    public ushort ServerPort { get; set; }
    public int NextState { get; set; }

    private class Impl : PacketSetProtocol
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(ProtocolVersion);
                    writer.WriteString(ServerHost);
                    writer.WriteUnsignedShort(ServerPort);
                    writer.WriteVarInt(NextState);
                    break;
                }
            }
        }
    }
}