using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Humanizer;
using MinecraftData;
using Protodef;
using Protodef.Enumerable;
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
            var protocol = await DeserializeProtocolAsync(item.Value.Path);
            if (item.Value.MinecraftVersions.Contains("1.21"))
                Debugger.Break();
            //ProtocolValidator.Validate(protocol, item.Value);
        }
    }


    static async Task<ProtodefProtocol> DeserializeProtocolAsync(AbsolutePath path)
    {
        var json = await path.ReadAllTextAsync();
        return ProtodefProtocol.Deserialize(json);
    }
}

public static class ProtocolValidator
{
    public static void Validate(ProtodefProtocol protocol, ProtocolInfo info)
    {
        foreach (var ns in protocol.EnumerateNamespaces())
        {
            var packets = ns.Types.Keys.Where(x => x.StartsWith("packet_"));

            var container = ns.Types["packet"] as ProtodefContainer;


            var mapper = container["params"] as ProtodefSwitch;

            foreach (var packet in packets)
            {
                if (!Contains(mapper, packet))
                {
                    throw new Exception(
                        $"Packet {packet} does not contain in protocol {info.Path.RelativeTo(MinecraftPaths.DataPath)}");
                }
            }
        }
    }

    private static bool Contains(ProtodefSwitch sw, string name)
    {
        return sw.Fields.Values.Cast<ProtodefCustomType>().Any(x => x.Name == name);
    }
}