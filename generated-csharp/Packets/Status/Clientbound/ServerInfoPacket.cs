using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class ServerInfoPacket : IProtocolType<ServerInfoPacket>
{
    public string Response { get; }

    public ServerInfoPacket(string response)
    {
        Response = response;
    }

    public static ServerInfoPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<ServerInfoPacket>(protocolVersion);
        var response = reader.ReadString();
        return new ServerInfoPacket(response);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<ServerInfoPacket>(protocolVersion);
        writer.WriteString(Response);
    }
}
