public abstract partial class PacketTileEntityData
{
    public V735_756Fields? V735_756 { get; set; }
    public V757_763Fields? V757_763 { get; set; }
    public V764_772Fields? V764_772 { get; set; }

    public partial struct V735_756Fields
    {
        public Position Location { get; set; }
        public byte Action { get; set; }
        public NbtTag? NbtData { get; set; }
    }

    public partial struct V757_763Fields
    {
        public Position Location { get; set; }
        public int Action { get; set; }
        public NbtTag? NbtData { get; set; }
    }

    public partial struct V764_772Fields
    {
        public Position Location { get; set; }
        public int Action { get; set; }
        public NbtTag? NbtData { get; set; }
    }

    private class Impl : PacketTileEntityData
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 756:
                {
                    var v735_756 = V735_756.GetValueOrDefault();
                    writer.WriteType<Position>(v735_756.Location, protocolVersion);
                    writer.WriteUnsignedByte(v735_756.Action);
                    writer.WriteType<optionalNbt>(v735_756.NbtData, protocolVersion);
                    break;
                }

                case >= 757 and <= 763:
                {
                    var v757_763 = V757_763.GetValueOrDefault();
                    writer.WriteType<Position>(v757_763.Location, protocolVersion);
                    writer.WriteVarInt(v757_763.Action);
                    writer.WriteType<optionalNbt>(v757_763.NbtData, protocolVersion);
                    break;
                }

                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteType<Position>(v764_772.Location, protocolVersion);
                    writer.WriteVarInt(v764_772.Action);
                    writer.WriteType<anonOptionalNbt>(v764_772.NbtData, protocolVersion);
                    break;
                }
            }
        }
    }
}