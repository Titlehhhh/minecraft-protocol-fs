public abstract partial class PacketUpdateCommandBlock
{
    public Position Location { get; set; }
    public string Command { get; set; }
    public int Mode { get; set; }
    public byte Flags { get; set; }

    private class Impl : PacketUpdateCommandBlock
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteString(Command);
                    writer.WriteVarInt(Mode);
                    writer.WriteUnsignedByte(Flags);
                    break;
                }
            }
        }
    }
}