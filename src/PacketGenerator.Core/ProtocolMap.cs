using TruePath;

namespace PacketGenerator.Core;

public class ProtocolMap
{
    public SortedDictionary<int, ProtocolInfo> Protocols { get; } = new();

    public SortedDictionary<string, int> VersionToProtocol { get; } = new(StringComparer.OrdinalIgnoreCase);


    internal void AddProtocol(VersionFile file, AbsolutePath protocolPath)
    {
        int version = file.Version;
        string minecraftVersion = file.MinecraftVersion!;

        if (Protocols.TryGetValue(version, out var protocolInfo))
        {
            protocolInfo.MinecraftVersions.Add(minecraftVersion);
        }
        else
        {
            Protocols.Add(version, new ProtocolInfo(version,
                protocolPath, null, [minecraftVersion]));
        }

        VersionToProtocol.Add(minecraftVersion, version);
    }
}