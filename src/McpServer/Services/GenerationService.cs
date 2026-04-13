using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Repositories;

namespace McpServer.Services;

/// <summary>
/// Singleton service that owns per-tier concurrency semaphores shared across all callers
/// (REST endpoints and MCP tools compete for the same slots).
/// Handles throttling, retry, and maps exceptions to <see cref="GenerationResult"/>.
/// </summary>
public sealed class GenerationService : IDisposable
{
    private const int MaxAttempts = 4;

    private readonly CodeGenerator       _codeGenerator;
    private readonly IProtocolRepository _protocol;
    private readonly ModelConfigService  _modelConfig;

    // Swapped atomically on config change — in-flight tasks hold a reference to the old dict
    // and Release() on their semaphore safely; the old dict is GC'd when all waiters are done.
    private volatile Dictionary<ComplexityTier, SemaphoreSlim> _semaphores;

    public GenerationService(
        CodeGenerator       codeGenerator,
        IProtocolRepository protocol,
        ModelConfigService  modelConfig)
    {
        _codeGenerator = codeGenerator;
        _protocol      = protocol;
        _modelConfig   = modelConfig;
        _semaphores    = BuildSemaphores(modelConfig.Config);

        modelConfig.ConfigChanged += OnConfigChanged;
    }

    private void OnConfigChanged(ModelConfig cfg) =>
        Interlocked.Exchange(ref _semaphores, BuildSemaphores(cfg));

    /// <summary>
    /// Generates a single packet with throttling and retry.
    /// Never throws (except <see cref="OperationCanceledException"/>).
    /// </summary>
    public async Task<GenerationResult> GenerateAsync(string id, CancellationToken ct)
    {
        var tier = ClassifyTier(id);
        var sem  = _semaphores[tier];
        await sem.WaitAsync(ct);
        try
        {
            return await GenerateWithRetryAsync(id, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>
    /// Generates a batch in parallel, yielding each result as soon as it completes.
    /// Uses <see cref="Task.WhenEach"/> — no Channel needed.
    /// </summary>
    public async IAsyncEnumerable<GenerationResult> GenerateBatchAsync(
        IEnumerable<string> ids,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var tasks = ids.Select(id => GenerateAsync(id, ct)).ToList();
        await foreach (var task in Task.WhenEach(tasks).WithCancellation(ct))
            yield return await task;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private ComplexityTier ClassifyTier(string id)
    {
        try
        {
            var def = _protocol.GetPacket(id);
            return _modelConfig.ClassifyTier(PacketComplexityScorer.Compute(def.History));
        }
        catch
        {
            return ComplexityTier.Easy;
        }
    }

    private async Task<GenerationResult> GenerateWithRetryAsync(string id, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var data = await _codeGenerator.GenerateAsync(id, ct);
                return GenerationResult.Ok(id, data);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ClientResultException ex) when (IsRetryable(ex) && attempt < MaxAttempts)
            {
                await Task.Delay(attempt * 2000, ct);
            }
            catch (ClientResultException ex)
            {
                return GenerationResult.Fail(id, MapHttpError(ex));
            }
            catch (Exception ex)
            {
                return GenerationResult.Fail(id, new GenerationError(GenerationErrorKind.Unknown, ex.Message));
            }
        }

        return GenerationResult.Fail(id, new GenerationError(GenerationErrorKind.Unknown, "Max retries exceeded"));
    }

    private static bool IsRetryable(ClientResultException ex)
    {
        if (ex.Status is 429 or 503 or 502) return true;
        if (ex.Status == 400)
        {
            var body = ex.GetRawResponse()?.Content.ToString() ?? "";
            return body.Contains("Context size",    StringComparison.OrdinalIgnoreCase)
                || body.Contains("context_length",  StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static GenerationError MapHttpError(ClientResultException ex)
    {
        var body    = ex.GetRawResponse()?.Content.ToString();
        var message = string.IsNullOrWhiteSpace(body) ? ex.Message : $"{ex.Message} | {body}";

        var kind = ex.Status switch
        {
            429 => GenerationErrorKind.RateLimited,
            400 when body?.Contains("Context size",   StringComparison.OrdinalIgnoreCase) == true
                  || body?.Contains("context_length", StringComparison.OrdinalIgnoreCase) == true
                => GenerationErrorKind.ContextTooLarge,
            _ => GenerationErrorKind.ApiError,
        };

        return new GenerationError(kind, $"HTTP {ex.Status}: {message}");
    }

    private static Dictionary<ComplexityTier, SemaphoreSlim> BuildSemaphores(ModelConfig cfg) =>
        Enum.GetValues<ComplexityTier>().ToDictionary(
            t => t,
            t => new SemaphoreSlim(Math.Max(1, t switch
            {
                ComplexityTier.Tiny   => cfg.Tiny.MaxConcurrency,
                ComplexityTier.Easy   => cfg.Easy.MaxConcurrency,
                ComplexityTier.Medium => cfg.Medium.MaxConcurrency,
                ComplexityTier.Heavy  => cfg.Heavy.MaxConcurrency,
                _                     => 4,
            })));

    public void Dispose()
    {
        _modelConfig.ConfigChanged -= OnConfigChanged;
        foreach (var s in _semaphores.Values) s.Dispose();
    }
}
