using System.Collections.Generic;

namespace McpServer.Repositories;

public interface IProtocolRepository
{
    ProtocolRange GetSupportedProtocols();

    IEnumerable<string> GetTypes();
    IEnumerable<string> GetPackets();

    IEnumerable<string> GetPacketMappers();
}