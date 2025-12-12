public abstract partial class PacketTransaction
{
    public sbyte WindowId { get; set; }
    public short Action { get; set; }
    public bool Accepted { get; set; }

    private class Impl : PacketTransaction
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 754:
                {
                    var v735_754 = V735_754.GetValueOrDefault();
                    writer.WriteSignedByte(WindowId);
                    writer.WriteSignedShort(Action);
                    writer.WriteBool(Accepted);
                    break;
                }
            }
        }
    }
}