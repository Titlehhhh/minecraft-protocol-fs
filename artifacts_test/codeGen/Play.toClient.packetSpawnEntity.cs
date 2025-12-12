public abstract partial class PacketSpawnEntity
{
    public int EntityId { get; set; }
    public int Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public sbyte Pitch { get; set; }
    public sbyte Yaw { get; set; }
    public short VelocityX { get; set; }
    public short VelocityY { get; set; }
    public short VelocityZ { get; set; }
    public V735_758Fields? V735_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V735_758Fields
    {
        public Guid ObjectUUID { get; set; }
        public int ObjectData { get; set; }
    }

    public partial struct V759_772Fields
    {
        public Guid ObjectUUID { get; set; }
        public sbyte HeadPitch { get; set; }
        public int ObjectData { get; set; }
    }

    private class Impl : PacketSpawnEntity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteUUID(v735_758.ObjectUUID);
                    writer.WriteVarInt(Type);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteSignedByte(Pitch);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedInt(v735_758.ObjectData);
                    writer.WriteSignedShort(VelocityX);
                    writer.WriteSignedShort(VelocityY);
                    writer.WriteSignedShort(VelocityZ);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteUUID(v759_772.ObjectUUID);
                    writer.WriteVarInt(Type);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteSignedByte(Pitch);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedByte(v759_772.HeadPitch);
                    writer.WriteVarInt(v759_772.ObjectData);
                    writer.WriteSignedShort(VelocityX);
                    writer.WriteSignedShort(VelocityY);
                    writer.WriteSignedShort(VelocityZ);
                    break;
                }
            }
        }
    }
}