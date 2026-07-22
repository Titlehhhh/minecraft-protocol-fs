using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(766, MinecraftVersion.LatestProtocol)]
public sealed partial class LoginCookieRequestPacket : IProtocolType<LoginCookieRequestPacket>
{
    public string Cookie { get; }

    public LoginCookieRequestPacket(string cookie)
    {
        Cookie = cookie;
    }

    public static LoginCookieRequestPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCookieRequestPacket>(protocolVersion);
        var cookie = reader.ReadString();
        return new LoginCookieRequestPacket(cookie);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCookieRequestPacket>(protocolVersion);
        writer.WriteString(Cookie);
    }
}
