public abstract partial class PacketUpdateSign
{
    public string Text1 { get; set; }
    public string Text2 { get; set; }
    public string Text3 { get; set; }
    public string Text4 { get; set; }
    public V735_762Fields? V735_762 { get; set; }
    public V763_772Fields? V763_772 { get; set; }

    public partial struct V735_762Fields
    {
        public Position Location { get; set; }
    }

    public partial struct V763_772Fields
    {
        public Position Location { get; set; }
        public bool IsFrontText { get; set; }
    }

    private class Impl : PacketUpdateSign
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 762:
                {
                    var v735_762 = V735_762.GetValueOrDefault();
                    writer.WriteType<Position>(v735_762.Location, protocolVersion);
                    writer.WriteString(Text1);
                    writer.WriteString(Text2);
                    writer.WriteString(Text3);
                    writer.WriteString(Text4);
                    break;
                }

                case >= 763 and <= 772:
                {
                    var v763_772 = V763_772.GetValueOrDefault();
                    writer.WriteType<Position>(v763_772.Location, protocolVersion);
                    writer.WriteBool(v763_772.IsFrontText);
                    writer.WriteString(Text1);
                    writer.WriteString(Text2);
                    writer.WriteString(Text3);
                    writer.WriteString(Text4);
                    break;
                }
            }
        }
    }
}