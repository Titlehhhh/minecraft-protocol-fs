public abstract partial class PacketPositionLook
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public V735_767Fields? V735_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_767Fields
    {
        public bool OnGround { get; set; }
    }

    public partial struct V768_772Fields
    {
        public byte Flags { get; set; }
    }

    private class Impl : PacketPositionLook
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 767:
                {
                    var v735_767 = V735_767.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteBool(v735_767.OnGround);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteType<MovementFlags>(v768_772.Flags, protocolVersion);
                    break;
                }
            }
        }
    }
}