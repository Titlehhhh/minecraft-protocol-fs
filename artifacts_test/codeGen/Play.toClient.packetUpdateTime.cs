public abstract partial class PacketUpdateTime
{
    public long Age { get; set; }
    public long Time { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V768_772Fields
    {
        public bool TickDayTime { get; set; }
    }

    private class Impl : PacketUpdateTime
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 767:
                {
                    var v735_767 = V735_767.GetValueOrDefault();
                    writer.WriteSignedLong(Age);
                    writer.WriteSignedLong(Time);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteSignedLong(Age);
                    writer.WriteSignedLong(Time);
                    writer.WriteBool(v768_772.TickDayTime);
                    break;
                }
            }
        }
    }
}