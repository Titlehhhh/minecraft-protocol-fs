public abstract partial class PacketWorldBorderCenter
{
    public double X { get; set; }
    public double Z { get; set; }

    private class Impl : PacketWorldBorderCenter
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteDouble(X);
                    writer.WriteDouble(Z);
                    break;
                }
            }
        }
    }
}