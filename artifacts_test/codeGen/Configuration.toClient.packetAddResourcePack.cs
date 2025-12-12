public abstract partial class PacketAddResourcePack
{
    public Guid Uuid { get; set; }
    public string Url { get; set; }
    public string Hash { get; set; }
    public bool Forced { get; set; }
    public NbtTag? PromptMessage { get; set; }

    private class Impl : PacketAddResourcePack
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 765:
                {
                    var v765 = V765.GetValueOrDefault();
                    writer.WriteUUID(Uuid);
                    writer.WriteString(Url);
                    writer.WriteString(Hash);
                    writer.WriteBool(Forced);
                    writer.WriteOptional(PromptMessage, protocolVersion, static writer =>
                    {
                        writer.WriteType<NbtTag>(writer, protocolVersion);
                    });
                    break;
                }
            }
        }
    }
}