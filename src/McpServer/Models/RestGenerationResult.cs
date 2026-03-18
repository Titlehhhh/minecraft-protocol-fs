namespace McpServer.Models;

/// <summary>
/// Result returned by REST endpoints (/api/generate, /api/generate/batch).
/// Contains the generated code inline — suitable for UI display.
/// </summary>
public sealed class RestGenerationResult
{
    public string? Name         { get; init; }
    public string? Code         { get; init; }
    public long    ElapsedMs    { get; init; }
    public string? Model        { get; init; }
    public string? SystemPrompt { get; init; }   // set when packet is too complex — heavy packet
    public string? UserPrompt   { get; init; }
    public string? Error        { get; init; }   // per-item error used in batch responses

    // Estimated prompt token breakdown
    public int     SystemTokenCount { get; init; }
    public int     UserTokenCount   { get; init; }
    public int     TokenCount       { get; init; }   // = System + User
    public int     ComplexityScore  { get; init; }

    // Real usage from API response (null when returnToClaude or provider doesn't report)
    public long?   InputTokens     { get; init; }
    public long?   CachedTokens    { get; init; }
    public long?   OutputTokens    { get; init; }   // total output incl. reasoning
    public long?   ReasoningTokens { get; init; }   // thinking (subset of OutputTokens)
    public long?   TotalTokens     { get; init; }   // InputTokens + OutputTokens

    public static RestGenerationResult From(GenerationData d) => new()
    {
        Name             = d.Name,
        Code             = d.Code,
        ElapsedMs        = d.ElapsedMs,
        Model            = d.Model,
        SystemPrompt     = d.SystemPrompt,
        UserPrompt       = d.UserPrompt,
        SystemTokenCount = d.SystemTokenCount,
        UserTokenCount   = d.UserTokenCount,
        TokenCount       = d.TokenCount,
        ComplexityScore  = d.ComplexityScore,
        InputTokens      = d.InputTokens,
        CachedTokens     = d.CachedTokens,
        OutputTokens     = d.OutputTokens,
        ReasoningTokens  = d.ReasoningTokens,
        TotalTokens      = d.TotalTokens,
    };
}
