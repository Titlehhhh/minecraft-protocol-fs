public abstract partial class PacketNbtQueryResponse
{
    public int TransactionId { get; set; }
    public V735_763Fields? V735_763 { get; set; }
    public V764_772Fields? V764_772 { get; set; }

    public partial struct V735_763Fields
    {
        public NbtTag? Nbt { get; set; }
    }

    public partial struct V764_772Fields
    {
        public NbtTag? Nbt { get; set; }
    }

    private class Impl : PacketNbtQueryResponse
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 763:
                {
                    var v735_763 = V735_763.GetValueOrDefault();
                    writer.WriteVarInt(TransactionId);
                    writer.WriteType<optionalNbt>(v735_763.Nbt, protocolVersion);
                    break;
                }

                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteVarInt(TransactionId);
                    writer.WriteType<anonOptionalNbt>(v764_772.Nbt, protocolVersion);
                    break;
                }
            }
        }
    }
}