using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;

namespace McpServer.Services;

public interface IPacketFileService
{
    /// <summary>
    /// Saves generated code to disk under outputBaseDir/namespace/direction/Name.cs.
    /// Returns the saved path, or null if code is empty or saving failed.
    /// Never throws.
    /// </summary>
    Task<string?> TrySaveAsync(GenerationData data, string packetId, string outputBaseDir, CancellationToken ct);
}
