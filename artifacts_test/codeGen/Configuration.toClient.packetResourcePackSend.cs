public abstract partial class PacketResourcePackSend
{
    public string Url { get; set; }
    public string Hash { get; set; }
    public bool Forced { get; set; }
    public string? PromptMessage { get; set; }

    private class Impl : PacketResourcePackSend
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 764:
                {
                    var v764 = V764.GetValueOrDefault();
                    writer.WriteString(Url);
                    writer.WriteString(Hash);
                    writer.WriteBool(Forced);
                    writer.WriteOptional(PromptMessage, protocolVersion, static writer =>
                    {
                        writer.WriteString(writer);
                    });
                    break;
                }
            }
        }
    }
}