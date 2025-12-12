public abstract partial class PacketCommonClearDialog
{
    private class Impl : PacketCommonClearDialog
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 771 and <= 772:
                {
                    break;
                }
            }
        }
    }
}