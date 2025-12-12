public abstract partial class PacketBlockPlace
{
    public int Hand { get; set; }
    public int Direction { get; set; }
    public float CursorX { get; set; }
    public float CursorY { get; set; }
    public float CursorZ { get; set; }
    public bool InsideBlock { get; set; }
    public V735_758Fields? V735_758 { get; set; }
    public V759_767Fields? V759_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_758Fields
    {
        public Position Location { get; set; }
    }

    public partial struct V759_767Fields
    {
        public Position Location { get; set; }
        public int Sequence { get; set; }
    }

    public partial struct V768_772Fields
    {
        public Position Location { get; set; }
        public bool WorldBorderHit { get; set; }
        public int Sequence { get; set; }
    }

    private class Impl : PacketBlockPlace
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    writer.WriteType<Position>(v735_758.Location, protocolVersion);
                    writer.WriteVarInt(Direction);
                    writer.WriteFloat(CursorX);
                    writer.WriteFloat(CursorY);
                    writer.WriteFloat(CursorZ);
                    writer.WriteBool(InsideBlock);
                    break;
                }

                case >= 759 and <= 767:
                {
                    var v759_767 = V759_767.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    writer.WriteType<Position>(v759_767.Location, protocolVersion);
                    writer.WriteVarInt(Direction);
                    writer.WriteFloat(CursorX);
                    writer.WriteFloat(CursorY);
                    writer.WriteFloat(CursorZ);
                    writer.WriteBool(InsideBlock);
                    writer.WriteVarInt(v759_767.Sequence);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    writer.WriteType<Position>(v768_772.Location, protocolVersion);
                    writer.WriteVarInt(Direction);
                    writer.WriteFloat(CursorX);
                    writer.WriteFloat(CursorY);
                    writer.WriteFloat(CursorZ);
                    writer.WriteBool(InsideBlock);
                    writer.WriteBool(v768_772.WorldBorderHit);
                    writer.WriteVarInt(v768_772.Sequence);
                    break;
                }
            }
        }
    }
}