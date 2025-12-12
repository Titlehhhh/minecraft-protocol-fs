public abstract partial class PacketEnterCombatEvent
{
    private class Impl : PacketEnterCombatEvent
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    break;
                }
            }
        }
    }
}