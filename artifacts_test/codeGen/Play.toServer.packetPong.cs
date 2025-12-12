public abstract partial class PacketPong
{
    public int Id { get; set; }

    private class Impl : PacketPong
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 755 and <= 772:
                {
                    var v755_772 = V755_772.GetValueOrDefault();
                    writer.WriteSignedInt(Id);
                    break;
                }
            }
        }
    }
}