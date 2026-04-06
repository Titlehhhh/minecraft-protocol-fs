namespace McpServer.Models;

public class AssessorConfig
{
    /// <summary>When false, structural scorer is used.</summary>
    public bool   Enabled  { get; set; } = false;
    public string Model    { get; set; } = "";
    /// <summary>
    /// OpenAI-compatible endpoint. Empty = OpenRouter.
    /// e.g. "http://localhost:1234/v1" for LM Studio.
    /// </summary>
    public string Endpoint { get; set; } = "";
    /// <summary>
    /// Max output tokens for the assessor call.
    /// Set to 1024+ when using a thinking model (reasoning tokens count against this budget).
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1024;
    /// <summary>Reasoning effort. Empty = off (default). Values: "low", "medium", "high".</summary>
    public string ReasoningEffort { get; set; } = "";
}

public class TierConfig
{
    public string Model           { get; set; } = "";
    /// <summary>Reasoning effort. Empty = off. Values: "low", "medium", "high", "xhigh".</summary>
    public string ReasoningEffort { get; set; } = "";
    /// <summary>
    /// Optional OpenAI-compatible endpoint override (e.g. "http://localhost:1234/v1" for LM Studio).
    /// Empty = use OpenRouter (https://openrouter.ai/api/v1/).
    /// </summary>
    public string Endpoint        { get; set; } = "";
}

public class ModelConfig
{
    public TierConfig Tiny   { get; set; } = new();
    public TierConfig Easy   { get; set; } = new() { Model = "openai/gpt-4o-mini" };
    public TierConfig Medium { get; set; } = new() { Model = "openai/gpt-4o-mini" };
    public TierConfig Heavy  { get; set; } = new();

    /// <summary>
    /// Complexity score ≤ this → Tiny tier (local model, e.g. LM Studio).
    /// 0 = disabled. Default 22: covers 1-range packets with ≤5 primitive fields and at most one option.
    /// Packets with arrays always score ≥25 and won't fall here.
    /// </summary>
    public int TinyComplexityThreshold  { get; set; } = 22;
    /// <summary>Complexity score ≤ this → Easy tier.</summary>
    public int EasyComplexityThreshold  { get; set; } = 20;
    /// <summary>Complexity score > this → Heavy tier.</summary>
    public int HeavyComplexityThreshold { get; set; } = 50;

    public AssessorConfig Assessor { get; set; } = new();

    public float  Temperature    { get; set; } = 0f;
    public int    MaxOutputTokens { get; set; } = 4096;
    /// <summary>Format for the packet schema sent in the prompt. "toon" or "json".</summary>
    public string InputFormat     { get; set; } = "toon";
}
