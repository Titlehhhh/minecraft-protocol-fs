public abstract partial class PacketCommonCustomClickAction
{
    public string Id { get; set; }
    public NbtTag? Nbt { get; set; }

    private class Impl : PacketCommonCustomClickAction
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 771 and <= 772:
                {
                    var v771_772 = V771_772.GetValueOrDefault();
                    writer.WriteString(Id);
                    writer.WriteOptional(Nbt, protocolVersion, static writer =>
                    {
                        writer.WriteType<NbtTag>(writer, protocolVersion);
                    });
                    break;
                }
            }
        }
    }
}