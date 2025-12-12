public abstract partial class PacketCompress
{
    public int Threshold { get; set; }

    private class Impl : PacketCompress
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(Threshold);
                    break;
                }
            }
        }
    }
}