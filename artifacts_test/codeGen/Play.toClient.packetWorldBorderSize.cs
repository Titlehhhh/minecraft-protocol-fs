public abstract partial class PacketWorldBorderSize
{
    public double Diameter { get; set; }

    private class Impl : PacketWorldBorderSize
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteDouble(Diameter);
                    break;
                }
            }
        }
    }
}