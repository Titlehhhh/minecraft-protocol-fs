public abstract partial class PacketSelectAdvancementTab
{
    public string? Id { get; set; }

    private class Impl : PacketSelectAdvancementTab
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteOptional(Id, protocolVersion, static writer =>
                    {
                        writer.WriteString(writer);
                    });
                    break;
                }
            }
        }
    }
}