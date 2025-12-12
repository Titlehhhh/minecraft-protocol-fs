public abstract partial class PacketEntityLook
{
    public int EntityId { get; set; }
    public sbyte Yaw { get; set; }
    public sbyte Pitch { get; set; }
    public bool OnGround { get; set; }

    private class Impl : PacketEntityLook
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedByte(Pitch);
                    writer.WriteBool(OnGround);
                    break;
                }
            }
        }
    }
}