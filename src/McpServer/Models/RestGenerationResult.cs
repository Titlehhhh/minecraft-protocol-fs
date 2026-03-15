namespace McpServer.Models;

/// <summary>
/// Result returned by REST endpoints (/api/generate, /api/generate/batch).
/// Contains the generated code inline — suitable for UI display.
/// </summary>
public sealed class RestGenerationResult
{
    public string? Name         { get; init; }
    public string? Code         { get; init; }
    public int     TokenCount   { get; init; }
    public long    ElapsedMs    { get; init; }
    public string? Model        { get; init; }
    public string? SystemPrompt { get; init; }   // set when packet is too complex — heavy packet
    public string? UserPrompt   { get; init; }
    public string? Error        { get; init; }   // per-item error used in batch responses

    public static RestGenerationResult From(GenerationData d) => new()
    {
        Name         = d.Name,
        Code         = d.Code,
        TokenCount   = d.TokenCount,
        ElapsedMs    = d.ElapsedMs,
        Model        = d.Model,
        SystemPrompt = d.SystemPrompt,
        UserPrompt   = d.UserPrompt,
    };
}
