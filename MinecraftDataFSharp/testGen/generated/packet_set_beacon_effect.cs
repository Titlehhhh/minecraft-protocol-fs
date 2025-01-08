namespace MinecraftDataFSharp
{
    public class SetBeaconEffect
    {
        public sealed class V393_758 : SetBeaconEffect
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 393 and <= 758;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int primaryEffect, int secondaryEffect)
            {
                writer.WriteVarInt(primaryEffect);
                writer.WriteVarInt(secondaryEffect);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, PrimaryEffect, SecondaryEffect);
            }

            public int PrimaryEffect { get; set; }
            public int SecondaryEffect { get; set; }
        }

        public sealed class V759_768 : SetBeaconEffect
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 759 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, int? primaryEffect, int? secondaryEffect)
            {
                writer.WriteBoolean(primaryEffect is not null);
                if (primaryEffect is not null)
                writer.WriteVarInt(primaryEffect!);
                writer.WriteBoolean(secondaryEffect is not null);
                if (secondaryEffect is not null)
                writer.WriteVarInt(secondaryEffect!);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, PrimaryEffect, SecondaryEffect);
            }

            public int? PrimaryEffect { get; set; }
            public int? SecondaryEffect { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V393_758.SupportedVersion(protocolVersion) || V759_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V393_758.SupportedVersion(protocolVersion))
            {
                V393_758.SerializeInternal(writer, default, default);
            }
            else
            {
                if (V759_768.SupportedVersion(protocolVersion))
                {
                    V759_768.SerializeInternal(writer, default, default);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}