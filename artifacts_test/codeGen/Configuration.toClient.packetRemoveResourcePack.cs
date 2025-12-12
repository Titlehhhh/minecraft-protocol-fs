public abstract partial class PacketRemoveResourcePack
{
    public Guid? Uuid { get; set; }

    private class Impl : PacketRemoveResourcePack
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 765:
                {
                    var v765 = V765.GetValueOrDefault();
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