using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class LoginPluginRequestPacket : IProtocolType<LoginPluginRequestPacket>
{
    public int MessageId { get; }
    public string Channel { get; }
    public byte[] Data { get; }

    public LoginPluginRequestPacket(int messageId, string channel, byte[] data)
    {
        MessageId = messageId;
        Channel = channel;
        Data = data;
    }

    public static LoginPluginRequestPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginPluginRequestPacket>(protocolVersion);
        var messageId = reader.ReadVarInt();
        var channel = reader.ReadString();
        var data = reader.ReadRestBytes();
        return new LoginPluginRequestPacket(messageId, channel, data);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginPluginRequestPacket>(protocolVersion);
        writer.WriteVarInt(MessageId);
        writer.WriteString(Channel);
        writer.WriteRestBytes(Data);
    }
}
