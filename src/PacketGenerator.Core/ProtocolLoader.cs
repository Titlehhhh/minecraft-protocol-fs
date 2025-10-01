using MinecraftData;
using Protodef;
using TruePath;
using TruePath.SystemIo;

namespace PacketGenerator.Core;

public static class ProtocolLoader
{
    public static async Task<ProtocolMap> LoadProtocolsAsync(int minVersion, int maxVersion)
    {
        var dict = await DataPathsHelper.GetPCDataPathsAsync();


        ProtocolMap protocolMap = new();
        foreach (var item in dict)
        {
            var protocolDir = MinecraftPaths.DataPath / item.Value.Protocol;
            var versionDir = MinecraftPaths.DataPath / item.Value.Version;


            var versionFilePath = versionDir / "version.json";

            var protocolFilePath = protocolDir / "protocol.json";

            var versionFile = await VersionFile.DeserializeAsync(versionFilePath);

            int ver = versionFile.Version;
            if (ver >= minVersion && ver <= maxVersion)
            {
                protocolMap.AddProtocol(versionFile, protocolFilePath);
            }
        }


        foreach (var item in protocolMap.Protocols)
        {
            ProtodefProtocol protocol;
            try
            {
                protocol = await DeserializeProtocolAsync(item.Value.Path);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Deserialize error in protocol {item.Value.Path.RelativeTo(MinecraftPaths.DataPath)}");
                Console.WriteLine(e.Message);
                throw;
            }

            ProtocolValidator.Validate(protocol, item.Value);
            item.Value.Protocol = protocol;
        }

        return protocolMap;
    }

    static async Task<ProtodefProtocol> DeserializeProtocolAsync(AbsolutePath path)
    {
        var json = await path.ReadAllTextAsync();
        return ProtodefProtocol.Deserialize(json);
    }
}