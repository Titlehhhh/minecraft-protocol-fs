namespace McpServer.Models;

public sealed record GenerationError(
    GenerationErrorKind Kind,
    string Message,
    string? Detail = null);
