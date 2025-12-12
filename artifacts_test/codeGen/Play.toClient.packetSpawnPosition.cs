public abstract partial class PacketSpawnPosition
{
    public V735_754Fields? V735_754 { get; set; }
    public V755_772Fields? V755_772 { get; set; }

    public partial struct V735_754Fields
    {
        public Position Location { get; set; }
    }

    public partial struct V755_772Fields
    {
        public Position Location { get; set; }
        public float Angle { get; set; }
    }

    private class Impl : PacketSpawnPosition
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteType<Position>(v735_754.Location, protocolVersion);
                    break;
                }

                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteType<Position>(v755_772.Location, protocolVersion);
                    writer.WriteFloat(v755_772.Angle);
                    break;
                }
            }
        }
    }
}