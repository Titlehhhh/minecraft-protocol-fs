public abstract partial class PacketChatPreview
{
    public int Query { get; set; }
    public string Message { get; set; }

    private class Impl : PacketChatPreview
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 759 and <= 760:
                {
                    var v759_760 = V759_760.GetValueOrDefault();
                    writer.WriteSignedInt(Query);
                    writer.WriteString(Message);
                    break;
                }
            }
        }
    }
}