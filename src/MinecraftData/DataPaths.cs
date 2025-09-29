using System.Text.Json.Serialization;
using TruePath;
using TruePath.SystemIo;
using PacketGenerator.Constants;

namespace MinecraftData;

internal class DataPaths
{
    [JsonPropertyName("pc")] public Dictionary<string, VersionData> PC { get; set; }
    [JsonPropertyName("bedrock")] public Dictionary<string, VersionData> Bedrock { get; set; }
}

public static class MinecraftPaths
{
    public static AbsolutePath DataPath = AbsolutePath.Create(MinecraftDataPaths.DataDir);
}