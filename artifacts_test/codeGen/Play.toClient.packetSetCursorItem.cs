public abstract partial class PacketSetCursorItem
{
    public V768Fields? V768 { get; set; }
    public V769_772Fields? V769_772 { get; set; }

    public partial struct V768Fields
    {
        public Slot? Contents { get; set; }
    }

    public partial struct V769_772Fields
    {
        public Slot Contents { get; set; }
    }

    private class Impl : PacketSetCursorItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 768:
                {
                    var v768 = V768.GetValueOrDefault();
                    writer.WriteOptional(v768.Contents, protocolVersion, static writer =>
                    {
                        writer.WriteType<Slot>(writer, protocolVersion);
                    });
                    break;
                }

                case >= 769 and <= 772:
                {
                    var v769_772 = V769_772.GetValueOrDefault();
                    writer.WriteType<Slot>(v769_772.Contents, protocolVersion);
                    break;
                }
            }
        }
    }
}