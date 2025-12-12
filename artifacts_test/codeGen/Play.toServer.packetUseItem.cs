public abstract partial class PacketUseItem
{
    public int Hand { get; set; }
    public V759_766Fields? V759_766 { get; set; }
    public V767_772Fields? V767_772 { get; set; }

    public partial struct V759_766Fields
    {
        public int Sequence { get; set; }
    }

    public partial struct V767_772Fields
    {
        public int Sequence { get; set; }
        public Vector2 Rotation { get; set; }
    }

    private class Impl : PacketUseItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    break;
                }

                case >= 759 and <= 766:
                {
                    var v759_766 = V759_766.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    writer.WriteVarInt(v759_766.Sequence);
                    break;
                }

                case >= 767 and <= 772:
                {
                    var v767_772 = V767_772.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    writer.WriteVarInt(v767_772.Sequence);
                    writer.WriteType<Vec2f>(v767_772.Rotation, protocolVersion);
                    break;
                }
            }
        }
    }
}