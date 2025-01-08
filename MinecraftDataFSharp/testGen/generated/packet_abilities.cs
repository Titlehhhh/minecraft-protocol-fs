namespace MinecraftDataFSharp
{
    public class Abilities
    {
        public sbyte Flags { get; set; }

        public sealed class V340_710 : Abilities
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 340 and <= 710;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte flags, float flyingSpeed, float walkingSpeed)
            {
                writer.WriteSignedByte(flags);
                writer.WriteFloat(flyingSpeed);
                writer.WriteFloat(walkingSpeed);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Flags, FlyingSpeed, WalkingSpeed);
            }

            public float FlyingSpeed { get; set; }
            public float WalkingSpeed { get; set; }
        }

        public sealed class V734_768 : Abilities
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 734 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, sbyte flags)
            {
                writer.WriteSignedByte(flags);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Flags);
            }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V340_710.SupportedVersion(protocolVersion) || V734_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V340_710.SupportedVersion(protocolVersion))
            {
                V340_710.SerializeInternal(writer, Flags, 0, 0);
            }
            else
            {
                if (V734_768.SupportedVersion(protocolVersion))
                {
                    V734_768.SerializeInternal(writer, Flags);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}