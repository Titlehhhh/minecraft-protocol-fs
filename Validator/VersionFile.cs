using System.Text.Json;
using System.Text.Json.Serialization;
using TruePath;
using TruePath.SystemIo;

namespace Validator;

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