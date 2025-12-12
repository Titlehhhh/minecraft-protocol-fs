public abstract partial class PacketConfigurationAcknowledged
{
    private class Impl : PacketConfigurationAcknowledged
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