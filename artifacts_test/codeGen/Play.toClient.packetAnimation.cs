public abstract partial class PacketAnimation
{
    public int EntityId { get; set; }
    public byte Animation { get; set; }

    private class Impl : PacketAnimation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteUnsignedByte(Animation);
                    break;
                }
            }
        }
    }
}