public abstract partial class PacketUnloadChunk
{
    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }

    private class Impl : PacketUnloadChunk
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 763:
                {
                    var v735_763 = V735_763.GetValueOrDefault();
                    writer.WriteSignedInt(ChunkX);
                    writer.WriteSignedInt(ChunkZ);
                    break;
                }

                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteSignedInt(ChunkZ);
                    writer.WriteSignedInt(ChunkX);
                    break;
                }
            }
        }
    }
}