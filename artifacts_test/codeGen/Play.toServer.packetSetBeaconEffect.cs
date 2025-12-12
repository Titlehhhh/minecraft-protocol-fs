public abstract partial class PacketSetBeaconEffect
{
    public V735_758Fields? V735_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V735_758Fields
    {
        public int PrimaryEffect { get; set; }
        public int SecondaryEffect { get; set; }
    }

    public partial struct V759_772Fields
    {
        public int? PrimaryEffect { get; set; }
        public int? SecondaryEffect { get; set; }
    }

    private class Impl : PacketSetBeaconEffect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteVarInt(v735_758.PrimaryEffect);
                    writer.WriteVarInt(v735_758.SecondaryEffect);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteOptional(v759_772.PrimaryEffect, protocolVersion, static writer =>
                    {
                        writer.WriteVarInt(writer);
                    });
                    writer.WriteOptional(v759_772.SecondaryEffect, protocolVersion, static writer =>
                    {
                        writer.WriteVarInt(writer);
                    });
                    break;
                }
            }
        }
    }
}