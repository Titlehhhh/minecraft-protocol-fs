public abstract partial class PacketEntityStatus
{
    public int EntityId { get; set; }
    public sbyte EntityStatus { get; set; }

    private class Impl : PacketEntityStatus
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedInt(EntityId);
                    writer.WriteSignedByte(EntityStatus);
                    break;
                }
            }
        }
    }
}