public abstract partial class PacketSetSlotState
{
    public int SlotId { get; set; }
    public bool State { get; set; }
    public V765_767Fields? V765_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V765_767Fields
    {
        public int WindowId { get; set; }
    }

    public partial struct V768_772Fields
    {
        public int WindowId { get; set; }
    }

    private class Impl : PacketSetSlotState
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 765 and <= 767:
                {
                    var v765_767 = V765_767.GetValueOrDefault();
                    writer.WriteVarInt(SlotId);
                    writer.WriteVarInt(v765_767.WindowId);
                    writer.WriteBool(State);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(SlotId);
                    writer.WriteType<ContainerID>(v768_772.WindowId, protocolVersion);
                    writer.WriteBool(State);
                    break;
                }
            }
        }
    }
}