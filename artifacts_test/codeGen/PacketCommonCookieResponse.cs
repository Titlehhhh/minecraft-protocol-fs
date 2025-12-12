public abstract partial class PacketCommonCookieResponse
{
    public string Key { get; set; }
    public byte[]? Value { get; set; }

    private class Impl : PacketCommonCookieResponse
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteString(Key);
                    writer.WriteOptional(Value, protocolVersion, static writer =>
                    {
                        writer.WriteBuffer<VarInt>(writer);
                    });
                    break;
                }
            }
        }
    }
}