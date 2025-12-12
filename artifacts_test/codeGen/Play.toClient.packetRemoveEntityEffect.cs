public abstract partial class PacketRemoveEntityEffect
{
    public int EntityId { get; set; }
    public V735_757Fields? V735_757 { get; set; }
    public V758_772Fields? V758_772 { get; set; }

    public partial struct V735_757Fields
    {
        public sbyte EffectId { get; set; }
    }

    public partial struct V758_772Fields
    {
        public int EffectId { get; set; }
    }

    private class Impl : PacketRemoveEntityEffect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 757:
                {
                    var v735_757 = V735_757.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedByte(v735_757.EffectId);
                    break;
                }

                case >= 758 and <= 772:
                {
                    var v758_772 = V758_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(v758_772.EffectId);
                    break;
                }
            }
        }
    }
}