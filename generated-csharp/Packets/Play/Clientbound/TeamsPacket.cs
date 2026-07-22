using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class TeamsPacket : IProtocolType<TeamsPacket>
{
    public string TeamName { get; }
    public TeamAction Action { get; }

    public TeamsPacket(string teamName, TeamAction action)
    {
        TeamName = teamName;
        Action = action;
    }

    public static TeamsPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<TeamsPacket>(protocolVersion);
        if (protocolVersion <= 764)
        {
            var teamName = reader.ReadString();
            var _mode = reader.ReadSignedByte();
            // TODO(codegen): ReadUnion ("_mode", "TeamAction", "Action")
            return new TeamsPacket(teamName, default!);
        }

        if (protocolVersion >= 771)
        {
            var teamName = reader.ReadString();
            var _mode = reader.ReadVarInt();
            // TODO(codegen): ReadUnion ("_mode", "TeamAction", "Action")
            return new TeamsPacket(teamName, default!);
        }

        throw new System.NotSupportedException($"TeamsPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<TeamsPacket>(protocolVersion);
        if (protocolVersion <= 764)
        {
            writer.WriteString(TeamName);
            // TODO(codegen): write wire-only '_mode' (derive from model)
            // TODO(codegen): ReadUnion ("_mode", "TeamAction", "Action")
            return;
        }

        if (protocolVersion >= 771)
        {
            writer.WriteString(TeamName);
            // TODO(codegen): write wire-only '_mode' (derive from model)
            // TODO(codegen): ReadUnion ("_mode", "TeamAction", "Action")
            return;
        }

        throw new System.NotSupportedException($"TeamsPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
