public abstract partial class PacketUpdateViewDistance
{
    public int ViewDistance { get; set; }

    private class Impl : PacketUpdateViewDistance
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(ViewDistance);
                    break;
                }
            }
        }
    }
}