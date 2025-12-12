public abstract partial class PacketLockDifficulty
{
    public bool Locked { get; set; }

    private class Impl : PacketLockDifficulty
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteBool(Locked);
                    break;
                }
            }
        }
    }
}