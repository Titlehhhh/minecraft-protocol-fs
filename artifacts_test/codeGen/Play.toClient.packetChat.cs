public abstract partial class PacketChat
{
    public string Message { get; set; }
    public sbyte Position { get; set; }
    public Guid Sender { get; set; }

    private class Impl : PacketChat
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteString(Message);
                    writer.WriteSignedByte(Position);
                    writer.WriteUUID(Sender);
                    break;
                }
            }
        }
    }
}