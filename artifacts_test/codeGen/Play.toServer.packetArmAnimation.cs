public abstract partial class PacketArmAnimation
{
    public int Hand { get; set; }

    private class Impl : PacketArmAnimation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(Hand);
                    break;
                }
            }
        }
    }
}