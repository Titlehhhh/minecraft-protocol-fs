public abstract partial class PacketWorldBorderWarningReach
{
    public int WarningBlocks { get; set; }

    private class Impl : PacketWorldBorderWarningReach
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteVarInt(WarningBlocks);
                    break;
                }
            }
        }
    }
}