public abstract partial class PacketTabComplete
{
    public int TransactionId { get; set; }
    public string Text { get; set; }

    private class Impl : PacketTabComplete
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(TransactionId);
                    writer.WriteString(Text);
                    break;
                }
            }
        }
    }
}