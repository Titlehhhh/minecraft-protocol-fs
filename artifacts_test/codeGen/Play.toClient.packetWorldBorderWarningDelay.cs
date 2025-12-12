public abstract partial class PacketWorldBorderWarningDelay
{
    public int WarningTime { get; set; }

    private class Impl : PacketWorldBorderWarningDelay
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteVarInt(WarningTime);
                    break;
                }
            }
        }
    }
}