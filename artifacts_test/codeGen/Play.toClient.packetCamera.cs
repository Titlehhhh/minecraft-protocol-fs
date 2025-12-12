public abstract partial class PacketCamera
{
    public int CameraId { get; set; }

    private class Impl : PacketCamera
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(CameraId);
                    break;
                }
            }
        }
    }
}