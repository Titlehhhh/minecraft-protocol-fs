public abstract partial class PacketScoreboardDisplayObjective
{
    public string Name { get; set; }
    public V735_763Fields? V735_763 { get; set; }
    public V764_772Fields? V764_772 { get; set; }

    public partial struct V735_763Fields
    {
        public sbyte Position { get; set; }
    }

    public partial struct V764_772Fields
    {
        public int Position { get; set; }
    }

    private class Impl : PacketScoreboardDisplayObjective
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 763:
                {
                    var v735_763 = V735_763.GetValueOrDefault();
                    writer.WriteSignedByte(v735_763.Position);
                    writer.WriteString(Name);
                    break;
                }

                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteVarInt(v764_772.Position);
                    writer.WriteString(Name);
                    break;
                }
            }
        }
    }
}