public abstract partial class PacketNamedSoundEffect
{
    public string SoundName { get; set; }
    public int SoundCategory { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public float Volume { get; set; }
    public float Pitch { get; set; }
    public V759_760Fields? V759_760 { get; set; }

    public partial struct V759_760Fields
    {
        public long Seed { get; set; }
    }

    private class Impl : PacketNamedSoundEffect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 758:
                {
                    var v735_758 = V735_758.GetValueOrDefault();
                    writer.WriteString(SoundName);
                    writer.WriteVarInt(SoundCategory);
                    writer.WriteSignedInt(X);
                    writer.WriteSignedInt(Y);
                    writer.WriteSignedInt(Z);
                    writer.WriteFloat(Volume);
                    writer.WriteFloat(Pitch);
                    break;
                }

                case >= 759 and <= 760:
                {
                    var v759_760 = V759_760.GetValueOrDefault();
                    writer.WriteString(SoundName);
                    writer.WriteVarInt(SoundCategory);
                    writer.WriteSignedInt(X);
                    writer.WriteSignedInt(Y);
                    writer.WriteSignedInt(Z);
                    writer.WriteFloat(Volume);
                    writer.WriteFloat(Pitch);
                    writer.WriteSignedLong(v759_760.Seed);
                    break;
                }
            }
        }
    }
}