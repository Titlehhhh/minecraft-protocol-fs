public abstract partial class PacketSystemChat
{
    public V759Fields? V759 { get; set; }
    public V760_764Fields? V760_764 { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V759Fields
    {
        public string Content { get; set; }
        public int Type { get; set; }
    }

    public partial struct V760_764Fields
    {
        public string Content { get; set; }
        public bool IsActionBar { get; set; }
    }

    public partial struct V765_772Fields
    {
        public NbtTag Content { get; set; }
        public bool IsActionBar { get; set; }
    }

    private class Impl : PacketSystemChat
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 759:
                {
                    var v759 = V759.GetValueOrDefault();
                    writer.WriteString(v759.Content);
                    writer.WriteVarInt(v759.Type);
                    break;
                }

                case >= 760 and <= 764:
                {
                    var v760_764 = V760_764.GetValueOrDefault();
                    writer.WriteString(v760_764.Content);
                    writer.WriteBool(v760_764.IsActionBar);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteType<NbtTag>(v765_772.Content, protocolVersion);
                    writer.WriteBool(v765_772.IsActionBar);
                    break;
                }
            }
        }
    }
}