public abstract partial class PacketEntityEffect
{
    public int EntityId { get; set; }
    public int Duration { get; set; }
    public V735_757Fields? V735_757 { get; set; }
    public V758Fields? V758 { get; set; }
    public V759_763Fields? V759_763 { get; set; }
    public V764_765Fields? V764_765 { get; set; }
    public V766_772Fields? V766_772 { get; set; }

    public partial struct V735_757Fields
    {
        public sbyte EffectId { get; set; }
        public sbyte Amplifier { get; set; }
        public sbyte HideParticles { get; set; }
    }

    public partial struct V758Fields
    {
        public int EffectId { get; set; }
        public sbyte Amplifier { get; set; }
        public sbyte HideParticles { get; set; }
    }

    public partial struct V759_763Fields
    {
        public int EffectId { get; set; }
        public sbyte Amplifier { get; set; }
        public sbyte HideParticles { get; set; }
        public NbtTag? FactorCodec { get; set; }
    }

    public partial struct V764_765Fields
    {
        public int EffectId { get; set; }
        public sbyte Amplifier { get; set; }
        public sbyte HideParticles { get; set; }
        public NbtTag? FactorCodec { get; set; }
    }

    public partial struct V766_772Fields
    {
        public int EffectId { get; set; }
        public int Amplifier { get; set; }
        public byte Flags { get; set; }
    }

    private class Impl : PacketEntityEffect
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 757:
                {
                    var v735_757 = V735_757.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteSignedByte(v735_757.EffectId);
                    writer.WriteSignedByte(v735_757.Amplifier);
                    writer.WriteVarInt(Duration);
                    writer.WriteSignedByte(v735_757.HideParticles);
                    break;
                }

                case 758:
                {
                    var v758 = V758.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(v758.EffectId);
                    writer.WriteSignedByte(v758.Amplifier);
                    writer.WriteVarInt(Duration);
                    writer.WriteSignedByte(v758.HideParticles);
                    break;
                }

                case >= 759 and <= 763:
                {
                    var v759_763 = V759_763.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(v759_763.EffectId);
                    writer.WriteSignedByte(v759_763.Amplifier);
                    writer.WriteVarInt(Duration);
                    writer.WriteSignedByte(v759_763.HideParticles);
                    writer.WriteOptional(v759_763.FactorCodec, protocolVersion, static writer =>
                    {
                        writer.WriteType<NbtTag>(writer, protocolVersion);
                    });
                    break;
                }

                case >= 764 and <= 765:
                {
                    var v764_765 = V764_765.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(v764_765.EffectId);
                    writer.WriteSignedByte(v764_765.Amplifier);
                    writer.WriteVarInt(Duration);
                    writer.WriteSignedByte(v764_765.HideParticles);
                    writer.WriteOptional(v764_765.FactorCodec, protocolVersion, static writer =>
                    {
                        writer.WriteType<NbtTag>(writer, protocolVersion);
                    });
                    break;
                }

                case >= 766 and <= 772:
                {
                    var v766_772 = V766_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(v766_772.EffectId);
                    writer.WriteVarInt(v766_772.Amplifier);
                    writer.WriteVarInt(Duration);
                    writer.WriteUnsignedByte(v766_772.Flags);
                    break;
                }
            }
        }
    }
}