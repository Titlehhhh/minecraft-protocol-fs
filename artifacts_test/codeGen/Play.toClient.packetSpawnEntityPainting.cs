public abstract partial class PacketSpawnEntityPainting
{
    public int EntityId { get; set; }
    public Guid EntityUUID { get; set; }
    public int Title { get; set; }
    public Position Location { get; set; }
    public byte Direction { get; set; }

    private class Impl : PacketSpawnEntityPainting
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
                    writer.WriteVarInt(Title);
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteUnsignedByte(Direction);
                    break;
                }
            }
        }
    }
}