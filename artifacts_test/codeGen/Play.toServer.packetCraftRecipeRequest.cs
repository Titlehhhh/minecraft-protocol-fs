public abstract partial class PacketCraftRecipeRequest
{
    public bool MakeAll { get; set; }
    public V735_766Fields? V735_766 { get; set; }
    public V767Fields? V767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V735_766Fields
    {
        public sbyte WindowId { get; set; }
        public string Recipe { get; set; }
    }

    public partial struct V767Fields
    {
        public int WindowId { get; set; }
        public string Recipe { get; set; }
    }

    public partial struct V768_772Fields
    {
        public int WindowId { get; set; }
        public int RecipeId { get; set; }
    }

    private class Impl : PacketCraftRecipeRequest
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 735 and <= 766:
                {
                    var v735_766 = V735_766.GetValueOrDefault();
                    writer.WriteSignedByte(v735_766.WindowId);
                    writer.WriteString(v735_766.Recipe);
                    writer.WriteBool(MakeAll);
                    break;
                }

                case 767:
                {
                    var v767 = V767.GetValueOrDefault();
                    writer.WriteType<ContainerID>(v767.WindowId, protocolVersion);
                    writer.WriteString(v767.Recipe);
                    writer.WriteBool(MakeAll);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteType<ContainerID>(v768_772.WindowId, protocolVersion);
                    writer.WriteVarInt(v768_772.RecipeId);
                    writer.WriteBool(MakeAll);
                    break;
                }
            }
        }
    }
}