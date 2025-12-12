public abstract partial class PacketChunkBatchFinished
{
    public int BatchSize { get; set; }

    private class Impl : PacketChunkBatchFinished
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteVarInt(BatchSize);
                    break;
                }
            }
        }
    }
}