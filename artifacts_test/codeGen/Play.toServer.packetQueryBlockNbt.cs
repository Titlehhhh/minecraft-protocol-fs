public abstract partial class PacketQueryBlockNbt
{
    public int TransactionId { get; set; }
    public Position Location { get; set; }

    private class Impl : PacketQueryBlockNbt
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(TransactionId);
                    writer.WriteType<Position>(Location, protocolVersion);
                    break;
                }
            }
        }
    }
}