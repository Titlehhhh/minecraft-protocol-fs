public abstract partial class PacketGenerateStructure
{
    public Position Location { get; set; }
    public int Levels { get; set; }
    public bool KeepJigsaws { get; set; }

    private class Impl : PacketGenerateStructure
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteVarInt(Levels);
                    writer.WriteBool(KeepJigsaws);
                    break;
                }
            }
        }
    }
}