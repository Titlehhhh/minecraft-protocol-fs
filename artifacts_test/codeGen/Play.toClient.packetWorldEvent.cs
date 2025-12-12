public abstract partial class PacketWorldEvent
{
    public int EffectId { get; set; }
    public Position Location { get; set; }
    public int Data { get; set; }
    public bool Global { get; set; }

    private class Impl : PacketWorldEvent
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedInt(EffectId);
                    writer.WriteType<Position>(Location, protocolVersion);
                    writer.WriteSignedInt(Data);
                    writer.WriteBool(Global);
                    break;
                }
            }
        }
    }
}