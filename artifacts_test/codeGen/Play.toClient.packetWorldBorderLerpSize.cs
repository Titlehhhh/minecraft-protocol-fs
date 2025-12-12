public abstract partial class PacketWorldBorderLerpSize
{
    public double OldDiameter { get; set; }
    public double NewDiameter { get; set; }
    public V755_758Fields? V755_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V755_758Fields
    {
        public long Speed { get; set; }
    }

    public partial struct V759_772Fields
    {
        public int Speed { get; set; }
    }

    private class Impl : PacketWorldBorderLerpSize
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 758:
                {
                    var v755_758 = V755_758.GetValueOrDefault();
                    writer.WriteDouble(OldDiameter);
                    writer.WriteDouble(NewDiameter);
                    writer.WriteVarLong(v755_758.Speed);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteDouble(OldDiameter);
                    writer.WriteDouble(NewDiameter);
                    writer.WriteVarInt(v759_772.Speed);
                    break;
                }
            }
        }
    }
}