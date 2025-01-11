namespace SandBoxLib;

public interface IServerPacket
{
    public void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion);


    public static virtual ServerPacket Id => throw new NotImplementedException();
    public static virtual bool VersionSupported(int protocolVersion) => throw new NotImplementedException();
}
