using System.Text.Json.Serialization;

namespace MinecraftData;

internal class DataPaths
{
    [JsonPropertyName("pc")] public Dictionary<string, VersionData> PC { get; set; }
    [JsonPropertyName("bedrock")] public Dictionary<string, VersionData> Bedrock { get; set; }
}