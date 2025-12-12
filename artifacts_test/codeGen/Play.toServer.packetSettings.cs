public abstract partial class PacketSettings
{
    public string Locale { get; set; }
    public sbyte ViewDistance { get; set; }
    public int ChatFlags { get; set; }
    public bool ChatColors { get; set; }
    public byte SkinParts { get; set; }
    public int MainHand { get; set; }
    public V755_756Fields? V755_756 { get; set; }
    public V757_765Fields? V757_765 { get; set; }

    public partial struct V755_756Fields
    {
        public bool DisableTextFiltering { get; set; }
    }

    public partial struct V757_765Fields
    {
        public bool EnableTextFiltering { get; set; }
        public bool EnableServerListing { get; set; }
    }

    private class Impl : PacketSettings
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteString(Locale);
                    writer.WriteSignedByte(ViewDistance);
                    writer.WriteVarInt(ChatFlags);
                    writer.WriteBool(ChatColors);
                    writer.WriteUnsignedByte(SkinParts);
                    writer.WriteVarInt(MainHand);
                    break;
                }

                case >= 755 and <= 756:
                {
                    var v755_756 = V755_756.GetValueOrDefault();
                    writer.WriteString(Locale);
                    writer.WriteSignedByte(ViewDistance);
                    writer.WriteVarInt(ChatFlags);
                    writer.WriteBool(ChatColors);
                    writer.WriteUnsignedByte(SkinParts);
                    writer.WriteVarInt(MainHand);
                    writer.WriteBool(v755_756.DisableTextFiltering);
                    break;
                }

                case >= 757 and <= 765:
                {
                    var v757_765 = V757_765.GetValueOrDefault();
                    writer.WriteString(Locale);
                    writer.WriteSignedByte(ViewDistance);
                    writer.WriteVarInt(ChatFlags);
                    writer.WriteBool(ChatColors);
                    writer.WriteUnsignedByte(SkinParts);
                    writer.WriteVarInt(MainHand);
                    writer.WriteBool(v757_765.EnableTextFiltering);
                    writer.WriteBool(v757_765.EnableServerListing);
                    break;
                }
            }
        }
    }
}