namespace McpServer.Models;

/// <summary>
/// Result returned to MCP callers (Claude).
/// Contains only a download Link — code is intentionally omitted to avoid flooding the context window.
/// The AI should use the link to curl the file to disk and optionally read it for verification.
/// </summary>
public sealed class McpGenerationResult
{
    public string? Name         { get; init; }
    public string? Link         { get; init; }
    public int     TokenCount   { get; init; }
    public long    ElapsedMs    { get; init; }
    public string? Model        { get; init; }
    public string? SystemPrompt { get; init; }   // set when packet is too complex — caller generates
    public string? UserPrompt   { get; init; }
    public string? Error        { get; init; }
    public string? SavedTo      { get; init; }   // set when outputBaseDir was provided and file was saved

    public static McpGenerationResult From(GenerationData d, string? savedTo = null) => new()
    {
        Name         = d.Name,
        Link         = d.Link,
        TokenCount   = d.TokenCount,
        ElapsedMs    = d.ElapsedMs,
        Model        = d.Model,
        SystemPrompt = d.SystemPrompt,
        UserPrompt   = d.UserPrompt,
        SavedTo      = savedTo,
    };
}
