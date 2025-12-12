public abstract partial class PacketStepTick
{
    public int TickSteps { get; set; }

    private class Impl : PacketStepTick
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteVarInt(TickSteps);
                    break;
                }
            }
        }
    }
}