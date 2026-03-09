namespace McpServer.Models;

public class GenerationResult
{
    public string? Name { get; set; }
    public string? Link { get; set; }
    public int TokenCount { get; set; }

    /// <summary>
    ///     Set when the packet is too complex for the cheap model.
    ///     The caller (Claude) should generate the code using this prompt.
    /// </summary>
    public string? SystemPrompt { get; set; }

    public string? UserPrompt { get; set; }
}