using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Helpers;
using McpServer.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Services;

public sealed class PacketFileService : IPacketFileService
{
    private readonly ILogger<PacketFileService> _logger;

    public PacketFileService(ILogger<PacketFileService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> TrySaveAsync(
        GenerationData data,
        string packetId,
        string outputBaseDir,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(data.Code))
            return null;

        try
        {
            var dir  = Path.Combine(outputBaseDir, PacketFileHelper.ResolveSubdir(packetId));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, data.Name + ".cs");
            await File.WriteAllTextAsync(path, data.Code, ct);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save packet file for '{PacketId}'", packetId);
            return null;
        }
    }
}
