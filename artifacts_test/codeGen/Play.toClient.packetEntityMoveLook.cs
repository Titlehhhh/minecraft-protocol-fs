public abstract partial class PacketEntityMoveLook
{
    public int EntityId { get; set; }
    public short DX { get; set; }
    public short DY { get; set; }
    public short DZ { get; set; }
    public sbyte Yaw { get; set; }
    public sbyte Pitch { get; set; }
    public bool OnGround { get; set; }

    private class Impl : PacketEntityMoveLook
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedShort(DX);
                    writer.WriteSignedShort(DY);
                    writer.WriteSignedShort(DZ);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedByte(Pitch);
                    writer.WriteBool(OnGround);
                    break;
                }
            }
        }
    }
}