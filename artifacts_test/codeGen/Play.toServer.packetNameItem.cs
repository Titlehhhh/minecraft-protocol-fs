public abstract partial class PacketNameItem
{
    public string Name { get; set; }

    private class Impl : PacketNameItem
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteString(Name);
                    break;
                }
            }
        }
    }
}