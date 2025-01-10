namespace MinecraftDataFSharp
{
    public class ResourcePackReceive
    {
        public int Result { get; set; }

        public sealed class V340_764 : ResourcePackReceive
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 764;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int result)
            {
                writer.WriteVarInt(result);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Result);
            }
        }

        public sealed class V765_768 : ResourcePackReceive
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Guid uuid, int result)
            {
                writer.WriteUUID(uuid);
                writer.WriteVarInt(result);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Uuid, Result);
            }

            public Guid Uuid { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_764.SupportedVersion(protocolVersion) || V765_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_764.SupportedVersion(protocolVersion))
            {
                V340_764.SerializeInternal(writer, Result);
            }
            else
            {
                if (V765_768.SupportedVersion(protocolVersion))
                {
                    V765_768.SerializeInternal(writer, default, Result);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}