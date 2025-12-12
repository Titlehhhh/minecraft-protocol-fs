public abstract partial class PacketSpawnEntityLiving
{
    public int EntityId { get; set; }
    public Guid EntityUUID { get; set; }
    public int Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public sbyte Yaw { get; set; }
    public sbyte Pitch { get; set; }
    public sbyte HeadPitch { get; set; }
    public short VelocityX { get; set; }
    public short VelocityY { get; set; }
    public short VelocityZ { get; set; }

    private class Impl : PacketSpawnEntityLiving
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteUUID(EntityUUID);
                    writer.WriteVarInt(Type);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedByte(Pitch);
                    writer.WriteSignedByte(HeadPitch);
                    writer.WriteSignedShort(VelocityX);
                    writer.WriteSignedShort(VelocityY);
                    writer.WriteSignedShort(VelocityZ);
                    break;
                }
            }
        }
    }
}