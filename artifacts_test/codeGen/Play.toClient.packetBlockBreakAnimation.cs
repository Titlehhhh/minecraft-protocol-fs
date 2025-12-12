public abstract partial class PacketBlockBreakAnimation
{
    public int EntityId { get; set; }
    public Position Location { get; set; }
    public sbyte DestroyStage { get; set; }

    private class Impl : PacketBlockBreakAnimation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteSignedByte(DestroyStage);
                    break;
                }
            }
        }
    }
}