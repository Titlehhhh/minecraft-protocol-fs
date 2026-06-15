namespace McpServer.Models;

public enum GenerationErrorKind
{
    RateLimited,
    ContextTooLarge,
    Configuration,
    ApiError,
    Cancelled,
    Unknown,
}
