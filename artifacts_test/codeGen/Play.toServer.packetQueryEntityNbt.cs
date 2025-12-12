public abstract partial class PacketQueryEntityNbt
{
    public int TransactionId { get; set; }
    public int EntityId { get; set; }

    private class Impl : PacketQueryEntityNbt
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(TransactionId);
                    writer.WriteVarInt(EntityId);
                    break;
                }
            }
        }
    }
}