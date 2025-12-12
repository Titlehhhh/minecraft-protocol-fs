public abstract partial class PacketSelectTrade
{
    public int Slot { get; set; }

    private class Impl : PacketSelectTrade
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(Slot);
                    break;
                }
            }
        }
    }
}