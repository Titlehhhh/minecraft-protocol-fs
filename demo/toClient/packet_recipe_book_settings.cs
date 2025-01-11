namespace MinecraftDataFSharp
{
    public abstract class RecipeBookSettings : IServerPacket
    {
        public bool CraftingGuiOpen { get; set; }
        public bool CraftingFilteringCraftable { get; set; }
        public bool SmeltingGuiOpen { get; set; }
        public bool SmeltingFilteringCraftable { get; set; }
        public bool BlastGuiOpen { get; set; }
        public bool BlastFilteringCraftable { get; set; }
        public bool SmokerGuiOpen { get; set; }
        public bool SmokerFilteringCraftable { get; set; }

        public sealed class V768_769 : RecipeBookSettings
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                CraftingGuiOpen = reader.ReadBoolean();
                CraftingFilteringCraftable = reader.ReadBoolean();
                SmeltingGuiOpen = reader.ReadBoolean();
                SmeltingFilteringCraftable = reader.ReadBoolean();
                BlastGuiOpen = reader.ReadBoolean();
                BlastFilteringCraftable = reader.ReadBoolean();
                SmokerGuiOpen = reader.ReadBoolean();
                SmokerFilteringCraftable = reader.ReadBoolean();
            }

            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 769;
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V768_769.SupportedVersion(protocolVersion);
        }

        public abstract void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);
    }
}