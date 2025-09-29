namespace SandBoxLib;

public class ProtocolNotSupportException(string packetName, int protocolVersion) : Exception;