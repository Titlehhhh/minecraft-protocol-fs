public abstract partial class PacketBlockAction
{
    public Position Location { get; set; }
    public byte Byte1 { get; set; }
    public byte Byte2 { get; set; }
    public int BlockId { get; set; }

    private class Impl : PacketBlockAction
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteUnsignedByte(Byte1);
                    writer.WriteUnsignedByte(Byte2);
                    writer.WriteVarInt(BlockId);
                    break;
                }
            }
        }
    }
}