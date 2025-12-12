public abstract partial class PacketDisplayedRecipe
{
    public V751_767Fields? V751_767 { get; set; }
    public V768_772Fields? V768_772 { get; set; }

    public partial struct V751_767Fields
    {
        public string RecipeId { get; set; }
    }

    public partial struct V768_772Fields
    {
        public int RecipeId { get; set; }
    }

    private class Impl : PacketDisplayedRecipe
    {
        public void Write(ref AbstractWriter writer, int protocolVersion)
        {
            switch (protocolVersion)
            {
                case >= 751 and <= 767:
                {
                    var v751_767 = V751_767.GetValueOrDefault();
                    writer.WriteString(v751_767.RecipeId);
                    break;
                }

                case >= 768 and <= 772:
                {
                    var v768_772 = V768_772.GetValueOrDefault();
                    writer.WriteVarInt(v768_772.RecipeId);
                    break;
                }
            }
        }
    }
}