public abstract partial class PacketFlying
{
    public V735_767Fields? V735_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_767Fields
    {
        public bool OnGround { get; set; }
    }

    public partial struct V768_772Fields
    {
        public byte Flags { get; set; }
    }

    private class Impl : PacketFlying
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 767:
                {
                    var v735_767 = V735_767.GetValueOrDefault();
                    writer.WriteBool(v735_767.OnGround);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteType<MovementFlags>(v768_772.Flags, protocolVersion);
                    break;
                }
            }
        }
    }
}