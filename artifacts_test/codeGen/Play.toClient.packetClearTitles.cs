public abstract partial class PacketClearTitles
{
    public bool Reset { get; set; }

    private class Impl : PacketClearTitles
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteBool(Reset);
                    break;
                }
            }
        }
    }
}