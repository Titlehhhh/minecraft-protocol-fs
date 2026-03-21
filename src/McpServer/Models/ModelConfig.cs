namespace McpServer.Models;

public class TierConfig
{
    public string Model           { get; set; } = "";
    /// <summary>Reasoning effort. Empty = off. Values: "low", "medium", "high", "xhigh".</summary>
    public string ReasoningEffort { get; set; } = "";
}

public class ModelConfig
{
    public TierConfig Easy   { get; set; } = new() { Model = "openai/gpt-4o-mini" };
    public TierConfig Medium { get; set; } = new() { Model = "openai/gpt-4o-mini" };
    public TierConfig Heavy  { get; set; } = new();

    /// <summary>Complexity score ≤ this → Easy tier.</summary>
    public int EasyComplexityThreshold  { get; set; } = 20;
    /// <summary>Complexity score > this → Heavy tier.</summary>
    public int HeavyComplexityThreshold { get; set; } = 50;

    public float  Temperature    { get; set; } = 0f;
    public int    MaxOutputTokens { get; set; } = 4096;
    /// <summary>Format for the packet schema sent in the prompt. "toon" or "json".</summary>
    public string InputFormat     { get; set; } = "toon";
}
