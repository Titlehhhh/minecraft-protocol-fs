public abstract partial class PacketSpectate
{
    public Guid Target { get; set; }

    private class Impl : PacketSpectate
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteUUID(Target);
                    break;
                }
            }
        }
    }
}