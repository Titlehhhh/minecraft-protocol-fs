public abstract partial class PacketSimulationDistance
{
    public int Distance { get; set; }

    private class Impl : PacketSimulationDistance
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 757 and <= 772:
                {
                    var v757_772 = V757_772.GetValueOrDefault();
                    writer.WriteVarInt(Distance);
                    break;
                }
            }
        }
    }
}