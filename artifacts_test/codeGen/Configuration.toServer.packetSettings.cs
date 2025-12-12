public abstract partial class PacketSettings
{
    public string Locale { get; set; }
    public sbyte ViewDistance { get; set; }
    public int ChatFlags { get; set; }
    public bool ChatColors { get; set; }
    public byte SkinParts { get; set; }
    public int MainHand { get; set; }
    public bool EnableTextFiltering { get; set; }
    public bool EnableServerListing { get; set; }

    private class Impl : PacketSettings
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 765:
                {
                    var v764_765 = V764_765.GetValueOrDefault();
                    writer.WriteString(Locale);
                    writer.WriteSignedByte(ViewDistance);
                    writer.WriteVarInt(ChatFlags);
                    writer.WriteBool(ChatColors);
                    writer.WriteUnsignedByte(SkinParts);
                    writer.WriteVarInt(MainHand);
                    writer.WriteBool(EnableTextFiltering);
                    writer.WriteBool(EnableServerListing);
                    break;
                }
            }
        }
    }
}