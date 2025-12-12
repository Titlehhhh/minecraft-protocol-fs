public abstract partial class PacketCommonStoreCookie
{
    public string Key { get; set; }
    public byte[] Value { get; set; }

    private class Impl : PacketCommonStoreCookie
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteString(Key);
                    writer.WriteBuffer<VarInt>(Value);
                    break;
                }
            }
        }
    }
}