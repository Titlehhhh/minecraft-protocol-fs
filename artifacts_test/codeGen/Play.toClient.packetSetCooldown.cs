public abstract partial class PacketSetCooldown
{
    public int CooldownTicks { get; set; }
    public V735_767Fields? V735_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_767Fields
    {
        public int ItemID { get; set; }
    }

    public partial struct V768_772Fields
    {
        public string CooldownGroup { get; set; }
    }

    private class Impl : PacketSetCooldown
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 767:
                {
                    var v735_767 = V735_767.GetValueOrDefault();
                    writer.WriteVarInt(v735_767.ItemID);
                    writer.WriteVarInt(CooldownTicks);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteString(v768_772.CooldownGroup);
                    writer.WriteVarInt(CooldownTicks);
                    break;
                }
            }
        }
    }
}