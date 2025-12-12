public abstract partial class PacketSetProjectilePower
{
    public int Id { get; set; }
    public V766Fields? V766 { get; set; }
    public V767_772Fields? V767_772 { get; set; }

    public partial struct V766Fields
    {
        public Vector3F64 Power { get; set; }
    }

    public partial struct V767_772Fields
    {
        public double AccelerationPower { get; set; }
    }

    private class Impl : PacketSetProjectilePower
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 766:
                {
                    var v766 = V766.GetValueOrDefault();
                    writer.WriteVarInt(Id);
                    writer.WriteType<Vec3f64>(v766.Power, protocolVersion);
                    break;
                }

                case >= 767 and <= 772:
                {
                    var v767_772 = V767_772.GetValueOrDefault();
                    writer.WriteVarInt(Id);
                    writer.WriteDouble(v767_772.AccelerationPower);
                    break;
                }
            }
        }
    }
}