public abstract partial class PacketPlayerLoaded
{
    private class Impl : PacketPlayerLoaded
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 769 and <= 772:
                {
                    break;
                }
            }
        }
    }
}