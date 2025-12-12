public abstract partial class PacketUpdateCommandBlockMinecart
{
    public int EntityId { get; set; }
    public string Command { get; set; }
    public bool TrackOutput { get; set; }

    private class Impl : PacketUpdateCommandBlockMinecart
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteString(Command);
                    writer.WriteBool(TrackOutput);
                    break;
                }
            }
        }
    }
}