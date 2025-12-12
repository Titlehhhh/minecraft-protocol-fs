public abstract partial class PacketFinishConfiguration
{
    private class Impl : PacketFinishConfiguration
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