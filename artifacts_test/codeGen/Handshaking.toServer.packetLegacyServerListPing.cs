public abstract partial class PacketLegacyServerListPing
{
    public byte Payload { get; set; }

    private class Impl : PacketLegacyServerListPing
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteUnsignedByte(Payload);
                    break;
                }
            }
        }
    }
}