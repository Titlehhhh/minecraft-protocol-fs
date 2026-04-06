using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using Protodef;

namespace McpServer.Services;

/// <summary>
/// Synchronous assessor based on PacketComplexityScorer heuristics.
/// Never allocates a Task — always returns via ValueTask.FromResult.
/// </summary>
public sealed class StructuralComplexityAssessor : IComplexityAssessor
{
    private readonly ModelConfigService _config;

    public StructuralComplexityAssessor(ModelConfigService config) => _config = config;

    public ValueTask<ComplexityAssessment> AssessAsync(
        Dictionary<ProtocolRange, ProtodefType?> history,
        CancellationToken ct = default)
    {
        var score = PacketComplexityScorer.Compute(history);
        var tier  = _config.ClassifyTier(score);
        return ValueTask.FromResult(new ComplexityAssessment(tier));
    }
}
