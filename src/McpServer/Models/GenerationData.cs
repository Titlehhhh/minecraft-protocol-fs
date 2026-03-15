namespace McpServer.Models;

/// <summary>Internal result from CodeGenerator — shared between REST and MCP layers.</summary>
public sealed class GenerationData
{
    public string?  Name         { get; init; }
    public string?  Code         { get; init; }   // actual generated C# code
    public string?  Link         { get; init; }   // /artifacts/{id}
    public int      TokenCount   { get; init; }
    public long     ElapsedMs    { get; init; }
    public string?  Model        { get; init; }
    public string?  SystemPrompt { get; init; }   // set when returnToClaude
    public string?  UserPrompt   { get; init; }   // set when returnToClaude
}
