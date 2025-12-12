public abstract partial class PacketPong
{
    public int Id { get; set; }

    private class Impl : PacketPong
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 764 and <= 772:
                {
                    var v764_772 = V764_772.GetValueOrDefault();
                    writer.WriteSignedInt(Id);
                    break;
                }
            }
        }
    }
}