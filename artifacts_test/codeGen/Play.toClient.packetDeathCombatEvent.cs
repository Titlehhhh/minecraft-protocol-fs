public abstract partial class PacketDeathCombatEvent
{
    public int PlayerId { get; set; }
    public V755_762Fields? V755_762 { get; set; }
    public V763_764Fields? V763_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V755_762Fields
    {
        public int EntityId { get; set; }
        public string Message { get; set; }
    }

    public partial struct V763_764Fields
    {
        public string Message { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag Message { get; set; }
    }

    private class Impl : PacketDeathCombatEvent
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 762:
                {
                    var v755_762 = V755_762.GetValueOrDefault();
                    writer.WriteVarInt(PlayerId);
                    writer.WriteSignedInt(v755_762.EntityId);
                    writer.WriteString(v755_762.Message);
                    break;
                }

                case >= 763 and <= 764:
                {
                    var v763_764 = V763_764.GetValueOrDefault();
                    writer.WriteVarInt(PlayerId);
                    writer.WriteString(v763_764.Message);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteVarInt(PlayerId);
                    writer.WriteType<NbtTag>(v765_772.Message, protocolVersion);
                    break;
                }
            }
        }
    }
}