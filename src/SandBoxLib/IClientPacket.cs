namespace SandBoxLib;

public interface IClientPacket
{
    public void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion);

    public virtual static ClientPacket Id => throw new NotImplementedException();

    public virtual static bool SupportedVersion(int protocolVersion) => throw new NotImplementedException();
}