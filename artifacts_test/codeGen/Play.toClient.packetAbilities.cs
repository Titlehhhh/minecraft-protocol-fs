public abstract partial class PacketAbilities
{
    public sbyte Flags { get; set; }
    public float FlyingSpeed { get; set; }
    public float WalkingSpeed { get; set; }

    private class Impl : PacketAbilities
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedByte(Flags);
                    writer.WriteFloat(FlyingSpeed);
                    writer.WriteFloat(WalkingSpeed);
                    break;
                }
            }
        }
    }
}