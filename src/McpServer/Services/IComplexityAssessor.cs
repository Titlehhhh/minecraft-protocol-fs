using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using Protodef;

namespace McpServer.Services;

/// <summary>
/// Determines complexity tier (and optionally a 0-100 score) for a packet.
/// Structural implementation is synchronous (ValueTask.FromResult).
/// LLM implementation is async and returns LlmScore + Reason.
/// </summary>
public interface IComplexityAssessor
{
    ValueTask<ComplexityAssessment> AssessAsync(
        Dictionary<ProtocolRange, ProtodefType?> history,
        CancellationToken ct = default);
}
