public abstract partial class PacketResourcePackSend
{
    public string Url { get; set; }
    public string Hash { get; set; }
    public V755_764Fields? V755_764 { get; set; }

    public partial struct V755_764Fields
    {
        public bool Forced { get; set; }
        public string? PromptMessage { get; set; }
    }

    private class Impl : PacketResourcePackSend
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteString(Url);
                    writer.WriteString(Hash);
                    break;
                }

                case >= 755 and <= 764:
                {
                    var v755_764 = V755_764.GetValueOrDefault();
                    writer.WriteString(Url);
                    writer.WriteString(Hash);
                    writer.WriteBool(v755_764.Forced);
                    writer.WriteOptional(v755_764.PromptMessage, protocolVersion, static writer =>
                    {
                        writer.WriteString(writer);
                    });
                    break;
                }
            }
        }
    }
}