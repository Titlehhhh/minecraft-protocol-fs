public abstract partial class PacketDebugSampleSubscription
{
    public int Type { get; set; }

    private class Impl : PacketDebugSampleSubscription
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteVarInt(Type);
                    break;
                }
            }
        }
    }
}