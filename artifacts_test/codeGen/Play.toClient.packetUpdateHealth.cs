public abstract partial class PacketUpdateHealth
{
    public float Health { get; set; }
    public int Food { get; set; }
    public float FoodSaturation { get; set; }

    private class Impl : PacketUpdateHealth
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteFloat(Health);
                    writer.WriteVarInt(Food);
                    writer.WriteFloat(FoodSaturation);
                    break;
                }
            }
        }
    }
}