namespace McpServer.Models;

/// <summary>
/// Result from IComplexityAssessor.
/// LlmScore and Reason are populated only by LlmComplexityAssessor.
/// </summary>
public record ComplexityAssessment(
    ComplexityTier Tier,
    int?           LlmScore = null,  // 0-100 rated by LLM; null when structural
    string?        Reason   = null
);
