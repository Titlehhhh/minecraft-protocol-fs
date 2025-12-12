public abstract partial class PacketCollect
{
    public int CollectedEntityId { get; set; }
    public int CollectorEntityId { get; set; }
    public int PickupItemCount { get; set; }

    private class Impl : PacketCollect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(CollectedEntityId);
                    writer.WriteVarInt(CollectorEntityId);
                    writer.WriteVarInt(PickupItemCount);
                    break;
                }
            }
        }
    }
}