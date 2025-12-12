public abstract partial class PacketEntity
{
    public int EntityId { get; set; }

    private class Impl : PacketEntity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    break;
                }
            }
        }
    }
}