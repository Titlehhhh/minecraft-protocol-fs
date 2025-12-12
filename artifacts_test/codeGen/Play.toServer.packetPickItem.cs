public abstract partial class PacketPickItem
{
    public int Slot { get; set; }

    private class Impl : PacketPickItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 768:
                {
                    var v735_768 = V735_768.GetValueOrDefault();
                    writer.WriteVarInt(Slot);
                    break;
                }
            }
        }
    }
}