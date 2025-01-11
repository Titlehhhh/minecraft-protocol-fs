namespace MinecraftDataFSharp
{
    public abstract class RecipeBookRemove : IServerPacket
    {
        public int[] RecipeIds { get; set; }

        public sealed class V768_769 : RecipeBookRemove
        {
            public override void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
            {
                RecipeIds = reader.ReadArray<int, int>(LengthFormat.VarInt, ReadDelegates.VarInt);
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