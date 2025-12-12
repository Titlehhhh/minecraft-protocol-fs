public abstract partial class PacketEndCombatEvent
{
    public int Duration { get; set; }
    public V755_762Fields? V755_762 { get; set; }

    public partial struct V755_762Fields
    {
        public int EntityId { get; set; }
    }

    private class Impl : PacketEndCombatEvent
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 762:
                {
                    var v755_762 = V755_762.GetValueOrDefault();
                    writer.WriteVarInt(Duration);
                    writer.WriteSignedInt(v755_762.EntityId);
                    break;
                }

                case >= 763 and <= 772:
                {
                    var v763_772 = V763_772.GetValueOrDefault();
                    writer.WriteVarInt(Duration);
                    break;
                }
            }
        }
    }
}