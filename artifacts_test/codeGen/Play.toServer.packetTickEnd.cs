public abstract partial class PacketTickEnd
{
    private class Impl : PacketTickEnd
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 768 and <= 772:
                {
                    break;
                }
            }
        }
    }
}