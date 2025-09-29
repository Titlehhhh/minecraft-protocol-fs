using System.Collections.Frozen;
using System.Text.Json;
using TruePath.SystemIo;

namespace MinecraftData;

public static class DataPathsHelper
{
    public static async Task<FrozenDictionary<string, VersionData>> GetPCDataPathsAsync()
    {
        var path = MinecraftPaths.DataPath / "dataPaths.json";
        
        await using var data = path.OpenRead();
        var deserialized = await JsonSerializer.DeserializeAsync<DataPaths>(data);

        return deserialized.PC.ToFrozenDictionary();
    }

    public static async Task<FrozenDictionary<string, VersionData>> GetBedrockDataPathsAsync()
    {
        await using var data = (MinecraftPaths.DataPath / "dataPaths.json").OpenRead();
        var deserialized = await JsonSerializer.DeserializeAsync<DataPaths>(data);

        return deserialized.Bedrock.ToFrozenDictionary();
    }
}