public abstract partial class PacketSteerBoat
{
    public bool LeftPaddle { get; set; }
    public bool RightPaddle { get; set; }

    private class Impl : PacketSteerBoat
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteBool(LeftPaddle);
                    writer.WriteBool(RightPaddle);
                    break;
                }
            }
        }
    }
}