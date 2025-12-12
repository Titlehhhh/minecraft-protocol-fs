public abstract partial class PacketResourcePackReceive
{
    public int Result { get; set; }
    public V765_772Fields? V765_772 { get; set; }

    public partial struct V765_772Fields
    {
        public Guid Uuid { get; set; }
    }

    private class Impl : PacketResourcePackReceive
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case 764:
                {
                    var v764 = V764.GetValueOrDefault();
                    writer.WriteVarInt(Result);
                    break;
                }

                case >= 765 and <= 772:
                {
                    var v765_772 = V765_772.GetValueOrDefault();
                    writer.WriteUUID(v765_772.Uuid);
                    writer.WriteVarInt(Result);
                    break;
                }
            }
        }
    }
}