namespace MinecraftDataFSharp
{
    public class CraftRecipeRequest
    {
        public bool MakeAll { get; set; }

        public sealed class V340 : CraftRecipeRequest
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 340;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte windowId, int recipe, bool makeAll)
            {
                writer.WriteSignedByte(windowId);
                writer.WriteVarInt(recipe);
                writer.WriteBoolean(makeAll);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, Recipe, MakeAll);
            }

            public sbyte WindowId { get; set; }
            public int Recipe { get; set; }
        }

        public sealed class V351_767 : CraftRecipeRequest
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 351 and <= 767;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte windowId, string recipe, bool makeAll)
            {
                writer.WriteSignedByte(windowId);
                writer.WriteString(recipe);
                writer.WriteBoolean(makeAll);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, Recipe, MakeAll);
            }

            public sbyte WindowId { get; set; }
            public string Recipe { get; set; }
        }

        public sealed class V768 : CraftRecipeRequest
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 768 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int windowId, int recipeId, bool makeAll)
            {
                writer.WriteVarInt(windowId);
                writer.WriteVarInt(recipeId);
                writer.WriteBoolean(makeAll);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, WindowId, RecipeId, MakeAll);
            }

            public int WindowId { get; set; }
            public int RecipeId { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340.SupportedVersion(protocolVersion) || V351_767.SupportedVersion(protocolVersion) || V768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340.SupportedVersion(protocolVersion))
            {
                V340.SerializeInternal(writer, 0, default, MakeAll);
            }
            else
            {
                if (V351_767.SupportedVersion(protocolVersion))
                {
                    V351_767.SerializeInternal(writer, 0, default, MakeAll);
                }
                else
                {
                    if (V768.SupportedVersion(protocolVersion))
                    {
                        V768.SerializeInternal(writer, default, default, MakeAll);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
        }
    }
}