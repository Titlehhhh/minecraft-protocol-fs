namespace McpServer.Models;

public enum ComplexityTier { Tiny, Easy, Medium, Heavy }

public static class ComplexityTierExtensions
{
    public static string ToLabel(this ComplexityTier tier) => tier switch
    {
        ComplexityTier.Tiny   => "tiny",
        ComplexityTier.Easy   => "easy",
        ComplexityTier.Medium => "medium",
        ComplexityTier.Heavy  => "heavy",
        _                     => "unknown",
    };
}
