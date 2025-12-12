public abstract partial class PacketPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public int TeleportId { get; set; }
    public V735_754Fields? V735_754 { get; set; }
    public V755_761Fields? V755_761 { get; set; }
    public V762_765Fields? V762_765 { get; set; }
    public V766_767Fields? V766_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_754Fields
    {
        public sbyte Flags { get; set; }
    }

    public partial struct V755_761Fields
    {
        public sbyte Flags { get; set; }
        public bool DismountVehicle { get; set; }
    }

    public partial struct V762_765Fields
    {
        public sbyte Flags { get; set; }
    }

    public partial struct V766_767Fields
    {
        public uint Flags { get; set; }
    }

    public partial struct V768_772Fields
    {
        public double Dx { get; set; }
        public double Dy { get; set; }
        public double Dz { get; set; }
        public uint Flags { get; set; }
    }

    private class Impl : PacketPosition
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteSignedByte(v735_754.Flags);
                    writer.WriteVarInt(TeleportId);
                    break;
                }

                case >= 755 and <= 761:
                {
                    var v755_761 = V755_761.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteSignedByte(v755_761.Flags);
                    writer.WriteVarInt(TeleportId);
                    writer.WriteBool(v755_761.DismountVehicle);
                    break;
                }

                case >= 762 and <= 765:
                {
                    var v762_765 = V762_765.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteSignedByte(v762_765.Flags);
                    writer.WriteVarInt(TeleportId);
                    break;
                }

                case >= 766 and <= 767:
                {
                    var v766_767 = V766_767.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteType<PositionUpdateRelatives>(v766_767.Flags, protocolVersion);
                    writer.WriteVarInt(TeleportId);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(TeleportId);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteDouble(v768_772.Dx);
                    writer.WriteDouble(v768_772.Dy);
                    writer.WriteDouble(v768_772.Dz);
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    writer.WriteType<PositionUpdateRelatives>(v768_772.Flags, protocolVersion);
                    break;
                }
            }
        }
    }
}