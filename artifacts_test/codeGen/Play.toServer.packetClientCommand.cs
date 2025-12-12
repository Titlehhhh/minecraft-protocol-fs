public abstract partial class PacketClientCommand
{
    public int ActionId { get; set; }

    private class Impl : PacketClientCommand
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(ActionId);
                    break;
                }
            }
        }
    }
}