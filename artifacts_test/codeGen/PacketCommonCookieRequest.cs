public abstract partial class PacketCommonCookieRequest
{
    public string Cookie { get; set; }

    private class Impl : PacketCommonCookieRequest
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteString(Cookie);
                    break;
                }
            }
        }
    }
}