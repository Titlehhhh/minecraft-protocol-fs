public abstract partial class PacketSteerVehicle
{
    public float Sideways { get; set; }
    public float Forward { get; set; }
    public byte Jump { get; set; }

    private class Impl : PacketSteerVehicle
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 767:
                {
                    var v735_767 = V735_767.GetValueOrDefault();
                    writer.WriteFloat(Sideways);
                    writer.WriteFloat(Forward);
                    writer.WriteUnsignedByte(Jump);
                    break;
                }
            }
        }
    }
}