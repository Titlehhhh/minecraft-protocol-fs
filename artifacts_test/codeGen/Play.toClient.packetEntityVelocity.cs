public abstract partial class PacketEntityVelocity
{
    public int EntityId { get; set; }
    public short VelocityX { get; set; }
    public short VelocityY { get; set; }
    public short VelocityZ { get; set; }

    private class Impl : PacketEntityVelocity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedShort(VelocityX);
                    writer.WriteSignedShort(VelocityY);
                    writer.WriteSignedShort(VelocityZ);
                    break;
                }
            }
        }
    }
}