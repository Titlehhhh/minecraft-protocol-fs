public abstract partial class PacketExperience
{
    public float ExperienceBar { get; set; }
    public int Level { get; set; }
    public int TotalExperience { get; set; }

    private class Impl : PacketExperience
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteFloat(ExperienceBar);
                    writer.WriteVarInt(Level);
                    writer.WriteVarInt(TotalExperience);
                    break;
                }
            }
        }
    }
}