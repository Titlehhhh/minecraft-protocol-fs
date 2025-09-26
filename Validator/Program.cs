using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        
        foreach (var item in dict)
        {
            var protocolDir = MinecraftPaths.DataPath / item.Value.Protocol;
            var versionDir = MinecraftPaths.DataPath / item.Value.Version;


            var versionFilePath = versionDir / "version.json";

            var versionFile = await VersionFile.DeserializeAsync(versionFilePath);

            if (versionFile.Version is <= maxVersion and >= minVersion)
            {
            }
        }

        
    }
}

public class VersionFile
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("minecraftVersion")] public string? MinecraftVersion { get; set; }
    [JsonPropertyName("majorVersion")] public string? MajorVersion { get; set; }
    [JsonPropertyName("releaseType")] public string? ReleaseType { get; set; }

    public static VersionFile Deserialize(string json)
    {
        return JsonSerializer.Deserialize<VersionFile>(json)!;
    }

    public static async Task<VersionFile> DeserializeAsync(AbsolutePath path)
    {
        await using var fs = path.OpenRead();
        return await DeserializeAsync(fs);
    }


    public static async Task<VersionFile> DeserializeAsync(Stream stream)
    {
        return (await JsonSerializer.DeserializeAsync<VersionFile>(stream))!;
    }
}