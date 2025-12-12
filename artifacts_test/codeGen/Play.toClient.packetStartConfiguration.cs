public abstract partial class PacketStartConfiguration
{
    private class Impl : PacketStartConfiguration
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