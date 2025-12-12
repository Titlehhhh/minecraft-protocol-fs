public abstract partial class PacketHurtAnimation
{
    public int EntityId { get; set; }
    public float Yaw { get; set; }

    private class Impl : PacketHurtAnimation
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 762 and <= 772:
                {
                    var v762_772 = V762_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteFloat(Yaw);
                    break;
                }
            }
        }
    }
}