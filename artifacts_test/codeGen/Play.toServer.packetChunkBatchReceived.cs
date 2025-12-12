public abstract partial class PacketChunkBatchReceived
{
    public float ChunksPerTick { get; set; }

    private class Impl : PacketChunkBatchReceived
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteFloat(ChunksPerTick);
                    break;
                }
            }
        }
    }
}