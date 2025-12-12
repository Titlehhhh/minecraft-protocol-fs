public abstract partial class PacketDisconnect
{
    public string Reason { get; set; }

    private class Impl : PacketDisconnect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteString(Reason);
                    break;
                }
            }
        }
    }
}