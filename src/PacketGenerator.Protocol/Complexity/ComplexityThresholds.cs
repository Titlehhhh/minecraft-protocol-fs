namespace PacketGenerator.Protocol.Complexity;

public sealed record ComplexityThresholds(
    int TinyComplexityThreshold = 22,
    int EasyComplexityThreshold = 20,
    int HeavyComplexityThreshold = 50)
{
    public ComplexityTier Classify(int complexityScore)
    {
        if (TinyComplexityThreshold > 0 && complexityScore <= TinyComplexityThreshold)
            return ComplexityTier.Tiny;
        if (complexityScore <= EasyComplexityThreshold)
            return ComplexityTier.Easy;
        if (complexityScore <= HeavyComplexityThreshold)
            return ComplexityTier.Medium;
        return ComplexityTier.Heavy;
    }
}
