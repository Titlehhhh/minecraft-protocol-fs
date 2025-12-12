public abstract partial class PacketCommonRemoveResourcePack
{
    public Guid? Uuid { get; set; }

    private class Impl : PacketCommonRemoveResourcePack
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteOptional(Uuid, protocolVersion, static writer =>
                    {
                        writer.WriteUUID(writer);
                    });
                    break;
                }
            }
        }
    }
}