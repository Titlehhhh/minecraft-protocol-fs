public abstract partial class PacketSpawnEntityExperienceOrb
{
    public int EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public short Count { get; set; }

    private class Impl : PacketSpawnEntityExperienceOrb
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 769:
                {
                    var v735_769 = V735_769.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteDouble(X);
                    writer.WriteDouble(Y);
                    writer.WriteDouble(Z);
                    writer.WriteSignedShort(Count);
                    break;
                }
            }
        }
    }
}