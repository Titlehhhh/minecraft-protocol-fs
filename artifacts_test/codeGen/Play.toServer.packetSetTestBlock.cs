public abstract partial class PacketSetTestBlock
{
    public Position Position { get; set; }
    public int Mode { get; set; }
    public string Message { get; set; }

    private class Impl : PacketSetTestBlock
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 770 and <= 772:
                {
                    var v770_772 = V770_772.GetValueOrDefault();
                    writer.WriteType<Position>(Position, protocolVersion);
                    writer.WriteVarInt(Mode);
                    writer.WriteString(Message);
                    break;
                }
            }
        }
    }
}