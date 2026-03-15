using System;
using System.ClientModel;
using System.IO;
using System.Text.Json;
using McpServer.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace McpServer.Services;

public class ModelConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _apiKey;
    private readonly string _configFilePath;
    private volatile ModelConfig _config;

    public ModelConfigService(string apiKey, string configFilePath, ModelConfig initial)
    {
        _apiKey = apiKey;
        _configFilePath = configFilePath;
        _config = initial;
    }

    public ModelConfig Config => _config;

    public void Update(ModelConfig config)
    {
        _config = config;
        try
        {
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(config, JsonOpts));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModelConfigService] Failed to persist config: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads config from file if it exists. Returns null if file not found or invalid.
    /// </summary>
    public static ModelConfig? TryLoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ModelConfig>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModelConfigService] Failed to load config from {path}: {ex.Message}");
            return null;
        }
    }

    public IChatClient CreateClient(string model) =>
        new OpenAIClient(
            new ApiKeyCredential(_apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1/") }
        ).GetChatClient(model).AsIChatClient();

    /// <summary>
    /// Returns (modelId, returnToClaude).
    /// returnToClaude=true means prompt should be returned to caller instead of sending to LLM.
    /// </summary>
    public (string Model, bool ReturnToClaude) PickModel(int tokenCount)
    {
        var cfg = _config;
        if (tokenCount <= cfg.SmallThreshold)
            return (cfg.SmallModel, false);
        if (tokenCount <= cfg.HeavyThreshold)
            return (string.IsNullOrEmpty(cfg.MediumModel) ? cfg.SmallModel : cfg.MediumModel, false);

        if (string.IsNullOrEmpty(cfg.HeavyModel))
            return (string.Empty, true);

        return (cfg.HeavyModel, false);
    }
}
