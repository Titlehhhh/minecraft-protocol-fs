public abstract partial class PacketInitializeWorldBorder
{
    public double X { get; set; }
    public double Z { get; set; }
    public double OldDiameter { get; set; }
    public double NewDiameter { get; set; }
    public int PortalTeleportBoundary { get; set; }
    public int WarningBlocks { get; set; }
    public int WarningTime { get; set; }
    public V755_758Fields? V755_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V755_758Fields
    {
        public long Speed { get; set; }
    }

    public partial struct V759_772Fields
    {
        public int Speed { get; set; }
    }

    private class Impl : PacketInitializeWorldBorder
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 758:
                {
                    var v755_758 = V755_758.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Z);
                    writer.WriteDouble(OldDiameter);
                    writer.WriteDouble(NewDiameter);
                    writer.WriteVarLong(v755_758.Speed);
                    writer.WriteVarInt(PortalTeleportBoundary);
                    writer.WriteVarInt(WarningBlocks);
                    writer.WriteVarInt(WarningTime);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Z);
                    writer.WriteDouble(OldDiameter);
                    writer.WriteDouble(NewDiameter);
                    writer.WriteVarInt(v759_772.Speed);
                    writer.WriteVarInt(PortalTeleportBoundary);
                    writer.WriteVarInt(WarningBlocks);
                    writer.WriteVarInt(WarningTime);
                    break;
                }
            }
        }
    }
}