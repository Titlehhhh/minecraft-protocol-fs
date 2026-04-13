namespace McpServer.Models;

public record PromptRequest(string? Id);
public record GenerateRequest(string? Id);
public record GenerateByNamespaceRequest(string? Ns);
public record GenerateByTierRequest(string? Tier);
public record GenerateBatchIdsRequest(string[]? Ids);
