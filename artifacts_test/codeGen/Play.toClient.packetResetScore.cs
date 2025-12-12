public abstract partial class PacketResetScore
{
    public string EntityName { get; set; }
    public string? ObjectiveName { get; set; }

    private class Impl : PacketResetScore
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteString(EntityName);
                    writer.WriteOptional(ObjectiveName, protocolVersion, static writer =>
                    {
                        writer.WriteString(writer);
                    });
                    break;
                }
            }
        }
    }
}