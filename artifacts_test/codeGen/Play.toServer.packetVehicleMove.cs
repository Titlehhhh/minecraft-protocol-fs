public abstract partial class PacketVehicleMove
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public V769_772Fields? V769_772 { get; set; }

    public partial struct V769_772Fields
    {
        public bool OnGround { get; set; }
    }

    private class Impl : PacketVehicleMove
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 768:
                {
                    var v735_768 = V735_768.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    break;
                }

                case >= 769 and <= 772:
                {
                    var v769_772 = V769_772.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteBool(v769_772.OnGround);
                    break;
                }
            }
        }
    }
}