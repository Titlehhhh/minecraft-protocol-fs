namespace MinecraftDataFSharp
{
    public class DisplayedRecipe
    {
        public sealed class V751_767 : DisplayedRecipe
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 751 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, string recipeId)
            {
                writer.WriteString(recipeId);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, RecipeId);
            }

            public string RecipeId { get; set; }
        }

        public sealed class V768 : DisplayedRecipe
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int recipeId)
            {
                writer.WriteVarInt(recipeId);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, RecipeId);
            }

            public int RecipeId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V751_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V751_767.SupportedVersion(protocolVersion))
            {
                V751_767.SerializeInternal(writer, default);
            }
            else
            {
                if (V768.SupportedVersion(protocolVersion))
                {
                    V768.SerializeInternal(writer, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}