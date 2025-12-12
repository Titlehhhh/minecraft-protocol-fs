public abstract partial class PacketSyncEntityPosition
{
    public int EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Dx { get; set; }
    public double Dy { get; set; }
    public double Dz { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public bool OnGround { get; set; }

    private class Impl : PacketSyncEntityPosition
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteDouble(Dx);
                    writer.WriteDouble(Dy);
                    writer.WriteDouble(Dz);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteBool(OnGround);
                    break;
                }
            }
        }
    }
}