using System.Collections.Generic;

namespace McpServer.Repositories;

public interface IProtocolRepository
{
    ProtocolRange GetSupportedProtocols();

    IEnumerable<string> GetTypes();
    Dictionary<string, Dictionary<string, PacketDefinition>> GetPackets();

    PacketDefinition GetPacket(string id);
    PacketDefinition GetPacket(string nameSpace, string name);

    IEnumerable<string> GetPacketMappers();
    
    TypeHistory GetTypeHistory(string id);
}