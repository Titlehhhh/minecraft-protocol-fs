using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
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

    public static int GetPacketId(int protocolVersion)
    {
        if (protocolVersion >= 735 && protocolVersion <= 736)
            return 0x4C;
        if (protocolVersion >= 751 && protocolVersion <= 754)
            return 0x4C;
        if (protocolVersion >= 755 && protocolVersion <= 755)
            return 0x55;
        if (protocolVersion >= 756 && protocolVersion <= 756)
            return 0x55;
        if (protocolVersion >= 757 && protocolVersion <= 758)
            return 0x55;
        if (protocolVersion >= 759 && protocolVersion <= 759)
            return 0x55;
        if (protocolVersion >= 760 && protocolVersion <= 760)
            return 0x58;
        if (protocolVersion >= 761 && protocolVersion <= 761)
            return 0x56;
        if (protocolVersion >= 762 && protocolVersion <= 763)
            return 0x5A;
        if (protocolVersion >= 764 && protocolVersion <= 764)
            return 0x5C;
        if (protocolVersion >= 765 && protocolVersion <= 765)
            return 0x5E;
        if (protocolVersion >= 766 && protocolVersion <= 766)
            return 0x60;
        if (protocolVersion >= 767 && protocolVersion <= 767)
            return 0x60;
        if (protocolVersion >= 768 && protocolVersion <= 769)
            return 0x67;
        if (protocolVersion >= 770 && protocolVersion <= 770)
            return 0x66;
        if (protocolVersion >= 771 && protocolVersion <= 772)
            return 0x66;
        throw new System.NotSupportedException($"No packet id for protocol {protocolVersion}.");
    }
}
