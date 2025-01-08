namespace MinecraftDataFSharp
{
    public class UpdateJigsawBlock
    {
        public Position Location { get; set; }
        public string FinalState { get; set; }

        public sealed class V477_578 : UpdateJigsawBlock
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 477 and <= 578;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, string attachmentType, string targetPool, string finalState)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteString(attachmentType);
                writer.WriteString(targetPool);
                writer.WriteString(finalState);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, AttachmentType, TargetPool, FinalState);
            }

            public string AttachmentType { get; set; }
            public string TargetPool { get; set; }
        }

        public sealed class V709_764 : UpdateJigsawBlock
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 709 and <= 764;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, string name, string target, string pool, string finalState, string jointType)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteString(name);
                writer.WriteString(target);
                writer.WriteString(pool);
                writer.WriteString(finalState);
                writer.WriteString(jointType);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Name, Target, Pool, FinalState, JointType);
            }

            public string Name { get; set; }
            public string Target { get; set; }
            public string Pool { get; set; }
            public string JointType { get; set; }
        }

        public sealed class V765_768 : UpdateJigsawBlock
        {
            public new static bool SupportedVersion(int protocolVersion)
            {
                return protocolVersion is >= 765 and <= 768;
            }

            internal static void SerializeInternal(MinecraftPrimitiveWriter writer, int protocolVersion, Position location, string name, string target, string pool, string finalState, string jointType, int selectionPriority, int placementPriority)
            {
                writer.WritePosition(location, protocolVersion);
                writer.WriteString(name);
                writer.WriteString(target);
                writer.WriteString(pool);
                writer.WriteString(finalState);
                writer.WriteString(jointType);
                writer.WriteVarInt(selectionPriority);
                writer.WriteVarInt(placementPriority);
            }

            public override void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
            {
                SerializeInternal(writer, protocolVersion, Location, Name, Target, Pool, FinalState, JointType, SelectionPriority, PlacementPriority);
            }

            public string Name { get; set; }
            public string Target { get; set; }
            public string Pool { get; set; }
            public string JointType { get; set; }
            public int SelectionPriority { get; set; }
            public int PlacementPriority { get; set; }
        }

        public static bool SupportedVersion(int protocolVersion)
        {
            return V477_578.SupportedVersion(protocolVersion) || V709_764.SupportedVersion(protocolVersion) || V765_768.SupportedVersion(protocolVersion);
        }

        public virtual void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            if (V477_578.SupportedVersion(protocolVersion))
            {
                V477_578.SerializeInternal(writer, Location, default, default, FinalState);
            }
            else
            {
                if (V709_764.SupportedVersion(protocolVersion))
                {
                    V709_764.SerializeInternal(writer, Location, default, default, default, FinalState, default);
                }
                else
                {
                    if (V765_768.SupportedVersion(protocolVersion))
                    {
                        V765_768.SerializeInternal(writer, Location, default, default, default, FinalState, default, default, default);
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