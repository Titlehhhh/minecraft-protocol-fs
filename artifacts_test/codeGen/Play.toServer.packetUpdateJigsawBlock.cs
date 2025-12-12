public abstract partial class PacketUpdateJigsawBlock
{
    public string Name { get; set; }
    public string Target { get; set; }
    public string Pool { get; set; }
    public string FinalState { get; set; }
    public string JointType { get; set; }
    public V735_764Fields? V735_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V735_764Fields
    {
        public Position Location { get; set; }
    }

    public partial struct V765_772Fields
    {
        public Position Location { get; set; }
        public int SelectionPriority { get; set; }
        public int PlacementPriority { get; set; }
    }

    private class Impl : PacketUpdateJigsawBlock
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 764:
                {
                    var v735_764 = V735_764.GetValueOrDefault();
                    writer.WriteType<Position>(v735_764.Location, protocolVersion);
                    writer.WriteString(Name);
                    writer.WriteString(Target);
                    writer.WriteString(Pool);
                    writer.WriteString(FinalState);
                    writer.WriteString(JointType);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteType<Position>(v765_772.Location, protocolVersion);
                    writer.WriteString(Name);
                    writer.WriteString(Target);
                    writer.WriteString(Pool);
                    writer.WriteString(FinalState);
                    writer.WriteString(JointType);
                    writer.WriteVarInt(v765_772.SelectionPriority);
                    writer.WriteVarInt(v765_772.PlacementPriority);
                    break;
                }
            }
        }
    }
}