public abstract partial class PacketShouldDisplayChatPreview
{
    public bool ShouldDisplayChatPreview { get; set; }

    private class Impl : PacketShouldDisplayChatPreview
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 759 and <= 760:
                {
                    var v759_760 = V759_760.GetValueOrDefault();
                    writer.WriteBool(ShouldDisplayChatPreview);
                    break;
                }
            }
        }
    }
}