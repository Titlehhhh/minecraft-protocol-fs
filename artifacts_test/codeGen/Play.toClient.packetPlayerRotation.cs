public abstract partial class PacketPlayerRotation
{
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    private class Impl : PacketPlayerRotation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteFloat(Yaw);
                    writer.WriteFloat(Pitch);
                    break;
                }
            }
        }
    }
}