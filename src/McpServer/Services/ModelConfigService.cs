using System;
using System.ClientModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace McpServer.Services;

public class ModelConfigService
{
    /// <summary>
    /// Raised after config is updated. Subscribers (e.g. GenerationService) use this
    /// to rebuild their semaphore pools without polling.
    /// </summary>
    public event Action<ModelConfig>? ConfigChanged;

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

    public async Task UpdateAsync(ModelConfig config, CancellationToken ct = default)
    {
        _config = config;
        ConfigChanged?.Invoke(config);
        try
        {
            await File.WriteAllTextAsync(_configFilePath, JsonSerializer.Serialize(config, JsonOpts), ct);
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

    public IChatClient CreateClient(string model, string endpoint = "")
    {
        var uri    = string.IsNullOrEmpty(endpoint)
            ? new Uri("https://openrouter.ai/api/v1/")
            : new Uri(endpoint.TrimEnd('/') + "/");
        var apiKey = string.IsNullOrEmpty(endpoint) ? _apiKey : "lm-studio";
        return new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = uri }
        ).GetChatClient(model).AsIChatClient();
    }

    /// <summary>
    /// Returns (model, reasoningEffort, returnToClaude, endpoint) based on structural complexity score.
    /// Delegates to PickModel(ComplexityTier).
    /// </summary>
    public (string Model, string ReasoningEffort, bool ReturnToClaude, string Endpoint) PickModel(int complexityScore)
        => PickModel(ClassifyTier(complexityScore));

    /// <summary>
    /// Returns (model, reasoningEffort, returnToClaude, endpoint) for the given tier.
    /// returnToClaude=true means prompt should be returned to caller instead of sending to LLM.
    /// endpoint is empty string for OpenRouter (default) or an override URL (e.g. LM Studio).
    /// </summary>
    public (string Model, string ReasoningEffort, bool ReturnToClaude, string Endpoint) PickModel(ComplexityTier tier)
    {
        var cfg = _config;
        return tier switch
        {
            ComplexityTier.Tiny   => PickTinyTier(cfg),
            ComplexityTier.Easy   => (cfg.Easy.Model, cfg.Easy.ReasoningEffort, false, cfg.Easy.Endpoint),
            ComplexityTier.Medium => string.IsNullOrEmpty(cfg.Medium.Model)
                ? (cfg.Easy.Model, cfg.Easy.ReasoningEffort, false, cfg.Easy.Endpoint)
                : (cfg.Medium.Model, cfg.Medium.ReasoningEffort, false, cfg.Medium.Endpoint),
            ComplexityTier.Heavy  => string.IsNullOrEmpty(cfg.Heavy.Model)
                ? (string.Empty, string.Empty, true, string.Empty)
                : (cfg.Heavy.Model, cfg.Heavy.ReasoningEffort, false, cfg.Heavy.Endpoint),
            _ => throw new ArgumentOutOfRangeException(nameof(tier)),
        };
    }

    /// <summary>
    /// Builds ChatOptions with optional reasoning effort control.
    /// reasoningEffort: "" = off, "low"/"medium"/"high"/"xhigh" = enabled at that level.
    /// </summary>
    public static ChatOptions BuildChatOptions(int maxOutputTokens, float temperature, string reasoningEffort, float? topP = null, long? seed = null)
    {
        if (!string.IsNullOrEmpty(reasoningEffort))
        {
#pragma warning disable OPENAI001
            var effort = reasoningEffort switch
            {
                "low"             => OpenAI.Chat.ChatReasoningEffortLevel.Low,
                "medium"          => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
                "high" or "xhigh" => OpenAI.Chat.ChatReasoningEffortLevel.High,
                _                 => OpenAI.Chat.ChatReasoningEffortLevel.Medium,
            };
            return new ChatOptions
            {
                MaxOutputTokens          = maxOutputTokens,
                Temperature              = temperature,
                TopP                     = topP,
                Seed                     = seed,
                RawRepresentationFactory = _ => new OpenAI.Chat.ChatCompletionOptions
                {
                    ReasoningEffortLevel = effort,
                    MaxOutputTokenCount  = maxOutputTokens,
                    Temperature          = temperature,
                    TopP                 = topP,
                    Seed                 = seed,
                }
            };
#pragma warning restore OPENAI001
        }

        return new ChatOptions
        {
            Temperature     = temperature,
            TopP            = topP,
            Seed            = seed,
            MaxOutputTokens = maxOutputTokens,
        };
    }

    private static (string Model, string ReasoningEffort, bool ReturnToClaude, string Endpoint) PickTinyTier(ModelConfig cfg)
    {
        var t = string.IsNullOrEmpty(cfg.Tiny.Model) ? cfg.Easy : cfg.Tiny;
        return (t.Model, t.ReasoningEffort, false, t.Endpoint);
    }

    /// <summary>Classifies a structural complexity score into a ComplexityTier.</summary>
    public ComplexityTier ClassifyTier(int complexityScore)
    {
        var cfg = _config;
        if (cfg.TinyComplexityThreshold > 0 && complexityScore <= cfg.TinyComplexityThreshold)
            return ComplexityTier.Tiny;
        if (complexityScore <= cfg.EasyComplexityThreshold) return ComplexityTier.Easy;
        if (complexityScore <= cfg.HeavyComplexityThreshold) return ComplexityTier.Medium;
        return ComplexityTier.Heavy;
    }
}
