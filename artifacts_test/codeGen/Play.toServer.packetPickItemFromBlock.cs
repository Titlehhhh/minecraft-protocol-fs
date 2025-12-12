public abstract partial class PacketPickItemFromBlock
{
    public Position Position { get; set; }
    public bool IncludeData { get; set; }

    private class Impl : PacketPickItemFromBlock
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 769 and <= 772:
                {
                    var v769_772 = V769_772.GetValueOrDefault();
                    writer.WriteType<Position>(Position, protocolVersion);
                    writer.WriteBool(IncludeData);
                    break;
                }
            }
        }
    }
}