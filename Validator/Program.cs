using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Humanizer;
using MinecraftData;
using Protodef;
using TruePath;
using TruePath.SystemIo;

namespace Validator;

class Program
{
    static async Task Main(string[] args)
    {
        var dict = await DataPathsHelper.GetPCDataPathsAsync();

        const int minVersion = 735; //1.16
        const int maxVersion = 772; //1.21.8

        ProtocolMap protocolMap = new();
        foreach (var item in dict)
        {
            var protocolDir = MinecraftPaths.DataPath / item.Value.Protocol;
            var versionDir = MinecraftPaths.DataPath / item.Value.Version;


            var versionFilePath = versionDir / "version.json";

            var protocolFilePath = protocolDir / "protocol.json";

            var versionFile = await VersionFile.DeserializeAsync(versionFilePath);

            if (versionFile.Version is <= maxVersion and >= minVersion)
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
                Console.WriteLine($"Deserialize error in protocol {item.Value.Path.RelativeTo(MinecraftPaths.DataPath)}");
                Console.WriteLine(e.Message);
                throw;
            }
            ProtocolValidator.Validate(protocol, item.Value);
            item.Value.Protocol = protocol;
        }
        
        
        
    }


    static async Task<ProtodefProtocol> DeserializeProtocolAsync(AbsolutePath path)
    {
        var json = await path.ReadAllTextAsync();
        return ProtodefProtocol.Deserialize(json);
    }
}