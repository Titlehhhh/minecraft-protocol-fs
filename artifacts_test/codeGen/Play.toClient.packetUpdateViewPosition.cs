public abstract partial class PacketUpdateViewPosition
{
    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }

    private class Impl : PacketUpdateViewPosition
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(ChunkX);
                    writer.WriteVarInt(ChunkZ);
                    break;
                }
            }
        }
    }
}