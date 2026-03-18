namespace McpServer.Models;

/// <summary>Internal result from CodeGenerator — shared between REST and MCP layers.</summary>
public sealed class GenerationData
{
    public string?  Name         { get; init; }
    public string?  Code         { get; init; }   // actual generated C# code
    public string?  Link         { get; init; }   // /artifacts/{id}
    public int      TokenCount       { get; init; }   // estimated prompt tokens (system+user)
    public int      SystemTokenCount { get; init; }   // estimated system prompt tokens
    public int      UserTokenCount   { get; init; }   // estimated user prompt tokens (= packet schema)
    public int      ComplexityScore  { get; init; }   // structural complexity score (used for model routing)
    public long     ElapsedMs    { get; init; }
    public string?  Model        { get; init; }
    public string?  SystemPrompt { get; init; }   // set when returnToClaude
    public string?  UserPrompt   { get; init; }   // set when returnToClaude

    // Real usage from API response (null when returnToClaude or provider doesn't report)
    public long?    InputTokens     { get; init; }
    public long?    CachedTokens    { get; init; }
    public long?    OutputTokens    { get; init; }  // total output incl. reasoning
    public long?    ReasoningTokens { get; init; }  // thinking tokens (subset of OutputTokens)
    public long?    TotalTokens     { get; init; }  // InputTokens + OutputTokens
}
