using System.Collections.Generic;

namespace McProtoFacts.Protocol.Repository;

/// <summary>One protocol number with the Minecraft releases that speak it.</summary>
public sealed record ProtocolVersionEntry(int Protocol, IReadOnlyList<string> Versions);

public interface IProtocolRepository
{
    ProtocolRange GetSupportedProtocols();

    /// <summary>Loaded protocol numbers with their Minecraft release names, ascending.</summary>
    IReadOnlyList<ProtocolVersionEntry> GetVersions();

    IEnumerable<string> GetTypes();
    IEnumerable<string> GetNativeTypes();
    Dictionary<string, Dictionary<string, PacketDefinition>> GetPackets();

    PacketDefinition GetPacket(string id);
    PacketDefinition GetPacket(string nameSpace, string name);

    IEnumerable<string> GetPacketMappers();

    TypeHistory GetTypeHistory(string id);
    bool ContainsPacket(string id);
}
