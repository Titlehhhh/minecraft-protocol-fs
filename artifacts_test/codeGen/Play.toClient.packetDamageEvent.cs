public abstract partial class PacketDamageEvent
{
    public int EntityId { get; set; }
    public int SourceTypeId { get; set; }
    public int SourceCauseId { get; set; }
    public int SourceDirectId { get; set; }
    public Vector3F64? SourcePosition { get; set; }

    private class Impl : PacketDamageEvent
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 762 and <= 772:
                {
                    var v762_772 = V762_772.GetValueOrDefault();
                    writer.WriteVarInt(EntityId);
                    writer.WriteVarInt(SourceTypeId);
                    writer.WriteVarInt(SourceCauseId);
                    writer.WriteVarInt(SourceDirectId);
                    writer.WriteOptional(SourcePosition, protocolVersion, static writer =>
                    {
                        writer.WriteType<Vec3f64>(writer, protocolVersion);
                    });
                    break;
                }
            }
        }
    }
}