using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using Humanizer;
using MinecraftData;
using PacketGenerator.Constants;
using Protodef;
using Protodef.Converters;
using Protodef.Enumerable;
using TruePath;
using TruePath.SystemIo;

namespace SandBoxApp;

class Program
{
    static async Task Main(string[] args)
    {
        
        //
        //
        //
        // var options = new JsonSerializerOptions
        // {
        //     WriteIndented = true,
        //     Converters = { new ProtodefTypeConverter() }
        // };
        //
        // AbsolutePath artifactsDir = AbsolutePath.Create(ArtifactsPaths.ArtifactsDir);
        //
        // artifactsDir.CreateDirectory();
        // artifactsDir.DeleteDirectoryRecursively();
        //
        // var typesPath = artifactsDir / "types";
        // var packetsPath = artifactsDir / "packets";
        //
        //
        // HashSet<string> allTypeNames = new();
        // HashSet<string> allPacketsNames = new();
        //
        // async Task WriteTypes(
        //     AbsolutePath dir,
        //     bool isPackets,
        //     IEnumerable<KeyValuePair<string, ProtodefType>> types)
        // {
        //     var filterTypes = types.Where(x => Filter(x, isPackets));
        //     if (filterTypes.Any())
        //         dir.CreateDirectory();
        //
        //
        //     foreach (var (k, v) in filterTypes)
        //     {
        //         var typeName = k.Pascalize();
        //         if (isPackets)
        //             allPacketsNames.Add(typeName);
        //         else
        //             allTypeNames.Add(typeName);
        //
        //
        //         var newDir = dir;
        //         if (v.IsSimpleTypeForGenerator())
        //         {
        //             newDir /= "primitive";
        //         }
        //         else
        //         {
        //             newDir /= "complex";
        //         }
        //
        //         newDir.CreateDirectory();
        //
        //         var path = newDir / $"{typeName}.json";
        //         if (path.Exists())
        //         {
        //             throw new Exception($"File {path} already exists");
        //         }
        //
        //         await path.WriteAllTextAsync(JsonSerializer.Serialize(v, options));
        //     }
        //
        //     return;
        //
        //     static bool Filter(KeyValuePair<string, ProtodefType> item, bool isPackets)
        //     {
        //         if (item.Key == "packet")
        //             return false;
        //         if (item.Value.IsCustom("native"))
        //             return false;
        //
        //
        //         if (isPackets)
        //         {
        //             if (!item.Key.StartsWith("packet_"))
        //                 return false;
        //         }
        //         else
        //         {
        //             if (item.Key.StartsWith("packet_"))
        //                 return false;
        //         }
        //
        //         return true;
        //     }
        // }
        //
        // (bool, AbsolutePath)[] paths = [(false, typesPath), (true, packetsPath)];
        //
        // foreach (var (isPackets, path) in paths)
        // {
        //     path.CreateDirectory();
        //     path.DeleteDirectoryRecursively();
        //     foreach (var (version, protocolInfo) in protocolMap.Protocols)
        //     {
        //         var dir = path / $"v{version}";
        //         dir.CreateDirectory();
        //
        //         var dirComplex = dir / "complex";
        //         dirComplex.CreateDirectory();
        //
        //         var dirSimple = dir / "primitive";
        //         dirSimple.CreateDirectory();
        //
        //         var verJson = dir / "version.json";
        //         await verJson.WriteAllTextAsync(JsonSerializer.Serialize(protocolInfo.MinecraftVersions));
        //
        //         await WriteTypes(dir, isPackets,
        //             protocolInfo.Protocol!.Types);
        //
        //         foreach (var ns in protocolInfo.Protocol.EnumerateNamespaces())
        //         {
        //             await WriteTypes(dir / ns.Fullname, isPackets, ns.Types);
        //         }
        //     }
        // }
        //
        // var allTxt = artifactsDir / "allTypes.txt";
        // await allTxt.WriteAllLinesAsync(allTypeNames.ToImmutableSortedSet());
        //
        // allTxt = artifactsDir / "allPackets.txt";
        // await allTxt.WriteAllLinesAsync(allPacketsNames.ToImmutableSortedSet());
    }


    
}