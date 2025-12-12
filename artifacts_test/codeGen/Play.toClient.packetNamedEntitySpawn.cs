public abstract partial class PacketNamedEntitySpawn
{
    public int EntityId { get; set; }
    public Guid PlayerUUID { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public sbyte Yaw { get; set; }
    public sbyte Pitch { get; set; }

    private class Impl : PacketNamedEntitySpawn
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 763:
                {
                    var v735_763 = V735_763.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteUUID(PlayerUUID);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteSignedByte(Yaw);
                    writer.WriteSignedByte(Pitch);
                    break;
                }
            }
        }
    }
}