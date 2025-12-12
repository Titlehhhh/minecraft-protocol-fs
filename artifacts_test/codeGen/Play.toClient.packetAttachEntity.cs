public abstract partial class PacketAttachEntity
{
    public int EntityId { get; set; }
    public int VehicleId { get; set; }

    private class Impl : PacketAttachEntity
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 772:
                {
                    var v735_772 = V735_772.GetValueOrDefault();
                    writer.WriteSignedInt(EntityId);
                    writer.WriteSignedInt(VehicleId);
                    break;
                }
            }
        }
    }
}