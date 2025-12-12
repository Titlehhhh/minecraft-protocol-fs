public abstract partial class PacketBlockChange
{
    public Position Location { get; set; }
    public int Type { get; set; }

    private class Impl : PacketBlockChange
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteVarInt(Type);
                    break;
                }
            }
        }
    }
}