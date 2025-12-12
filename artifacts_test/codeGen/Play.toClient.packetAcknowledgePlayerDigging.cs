public abstract partial class PacketAcknowledgePlayerDigging
{
    public V735_758Fields? V735_758 { get; set; }
    public V759_772Fields? V759_772 { get; set; }

    public partial struct V735_758Fields
    {
        public Position Location { get; set; }
        public int Block { get; set; }
        public int Status { get; set; }
        public bool Successful { get; set; }
    }

    public partial struct V759_772Fields
    {
        public int SequenceId { get; set; }
    }

    private class Impl : PacketAcknowledgePlayerDigging
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteType<Position>(v735_758.Location, protocolVersion);
                    writer.WriteVarInt(v735_758.Block);
                    writer.WriteVarInt(v735_758.Status);
                    writer.WriteBool(v735_758.Successful);
                    break;
                }

                case >= 759 and <= 772:
                {
                    var v759_772 = V759_772.GetValueOrDefault();
                    writer.WriteVarInt(v759_772.SequenceId);
                    break;
                }
            }
        }
    }
}