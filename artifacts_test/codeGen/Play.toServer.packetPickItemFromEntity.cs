public abstract partial class PacketPickItemFromEntity
{
    public int EntityId { get; set; }
    public bool IncludeData { get; set; }

    private class Impl : PacketPickItemFromEntity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 769 and <= 772:
                {
                    var v769_772 = V769_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteBool(IncludeData);
                    break;
                }
            }
        }
    }
}