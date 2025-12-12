public abstract partial class PacketPingStart
{
    private class Impl : PacketPingStart
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    break;
                }
            }
        }
    }
}