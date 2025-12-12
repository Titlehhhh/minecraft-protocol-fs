public abstract partial class PacketTeleportConfirm
{
    public int TeleportId { get; set; }

    private class Impl : PacketTeleportConfirm
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(TeleportId);
                    break;
                }
            }
        }
    }
}