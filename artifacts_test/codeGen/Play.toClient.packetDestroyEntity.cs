public abstract partial class PacketDestroyEntity
{
    public int EntityId { get; set; }

    private class Impl : PacketDestroyEntity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 755:
                {
                    var v755 = V755.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    break;
                }
            }
        }
    }
}