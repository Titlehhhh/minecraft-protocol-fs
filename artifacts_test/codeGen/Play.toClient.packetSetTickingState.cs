public abstract partial class PacketSetTickingState
{
    public float TickRate { get; set; }
    public bool IsFrozen { get; set; }

    private class Impl : PacketSetTickingState
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteFloat(TickRate);
                    writer.WriteBool(IsFrozen);
                    break;
                }
            }
        }
    }
}