using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(766, MinecraftVersion.LatestProtocol)]
public sealed partial class LoginCookieResponsePacket : IProtocolType<LoginCookieResponsePacket>
{
    public string Key { get; }
    public byte[]? Value { get; }

    public LoginCookieResponsePacket(string key, byte[]? value)
    {
        Key = key;
        Value = value;
    }

    public static LoginCookieResponsePacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCookieResponsePacket>(protocolVersion);
        var key = reader.ReadString();
        byte[]? value = null;
        if (reader.ReadBoolean())
            value = reader.ReadByteArray();
        return new LoginCookieResponsePacket(key, value);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<LoginCookieResponsePacket>(protocolVersion);
        writer.WriteString(Key);
        writer.WriteBoolean(Value is not null);
        if (Value is { } valueValue)
            writer.WriteByteArray(valueValue);
    }
}
