namespace McpServer.Models;

public class ModelConfig
{
    public string SmallModel { get; set; } = "openai/gpt-4o-mini";
    public string MediumModel { get; set; } = "openai/gpt-4o-mini";
    public string HeavyModel { get; set; } = "";
    public int SmallThreshold { get; set; } = 1500;
    public int HeavyThreshold { get; set; } = 4000;

    /// <summary>Complexity score ≤ this → SmallModel.</summary>
    public int SmallComplexityThreshold { get; set; } = 20;
    /// <summary>Complexity score > this → HeavyModel (or return to Claude if HeavyModel is empty).</summary>
    public int HeavyComplexityThreshold { get; set; } = 50;
    public float Temperature { get; set; } = 0f;
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Reasoning effort level for thinking models.
    /// Empty string = disabled. Values: "low", "medium", "high", "xhigh".
    /// When set, Temperature is ignored (not sent to model).
    /// </summary>
    public string ReasoningEffort { get; set; } = "";
    /// <summary>Format for the packet schema sent in the prompt. "toon" or "json".</summary>
    public string InputFormat { get; set; } = "toon";
}
