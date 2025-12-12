public abstract partial class PacketLoginAcknowledged
{
    private class Impl : PacketLoginAcknowledged
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