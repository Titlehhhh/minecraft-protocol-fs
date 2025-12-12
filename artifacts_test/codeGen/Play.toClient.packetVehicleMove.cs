public abstract partial class PacketVehicleMove
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    private class Impl : PacketVehicleMove
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    break;
                }
            }
        }
    }
}