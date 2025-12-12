public abstract partial class PacketResetChat
{
    private class Impl : PacketResetChat
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    break;
                }
            }
        }
    }
}