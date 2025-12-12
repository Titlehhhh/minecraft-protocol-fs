public abstract partial class PacketBlockDig
{
    public int Status { get; set; }
    public sbyte Face { get; set; }
    public V735_758Fields? V735_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V735_758Fields
    {
        public Position Location { get; set; }
    }

    public partial struct V759_772Fields
    {
        public Position Location { get; set; }
        public int Sequence { get; set; }
    }

    private class Impl : PacketBlockDig
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(Status);
                    writer.WriteType<Position>(v735_758.Location, protocolVersion);
                    writer.WriteSignedByte(Face);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteVarInt(Status);
                    writer.WriteType<Position>(v759_772.Location, protocolVersion);
                    writer.WriteSignedByte(Face);
                    writer.WriteVarInt(v759_772.Sequence);
                    break;
                }
            }
        }
    }
}