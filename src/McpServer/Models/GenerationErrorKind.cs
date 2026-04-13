namespace McpServer.Models;

public enum GenerationErrorKind
{
    RateLimited,
    ContextTooLarge,
    ApiError,
    Cancelled,
    Unknown,
}
