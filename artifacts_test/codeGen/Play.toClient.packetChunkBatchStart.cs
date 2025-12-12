public abstract partial class PacketChunkBatchStart
{
    private class Impl : PacketChunkBatchStart
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    break;
                }
            }
        }
    }
}