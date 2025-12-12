public abstract partial class PacketCommonAddResourcePack
{
    public Guid Uuid { get; set; }
    public string Url { get; set; }
    public string Hash { get; set; }
    public bool Forced { get; set; }
    public NbtTag? PromptMessage { get; set; }

    private class Impl : PacketCommonAddResourcePack
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
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