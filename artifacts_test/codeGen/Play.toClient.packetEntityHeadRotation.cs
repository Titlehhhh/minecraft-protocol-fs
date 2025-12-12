public abstract partial class PacketEntityHeadRotation
{
    public int EntityId { get; set; }
    public sbyte HeadYaw { get; set; }

    private class Impl : PacketEntityHeadRotation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedByte(HeadYaw);
                    break;
                }
            }
        }
    }
}