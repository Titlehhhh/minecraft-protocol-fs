using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Repositories;
using Microsoft.Extensions.Logging;

namespace McpServer.Services;

/// <summary>
/// Singleton service that owns per-tier concurrency semaphores shared across all callers
/// (REST endpoints and MCP tools compete for the same slots).
/// Handles throttling, retry, and maps exceptions to <see cref="GenerationResult"/>.
/// </summary>
public sealed class GenerationService : IDisposable
{
    private const int MaxAttempts = 4;

    private readonly CodeGenerator          _codeGenerator;
    private readonly IProtocolRepository    _protocol;
    private readonly ModelConfigService     _modelConfig;
    private readonly ILogger<GenerationService> _logger;

    // Swapped atomically on config change — in-flight tasks hold a reference to the old dict
    // and Release() on their semaphore safely; the old dict is GC'd when all waiters are done.
    private volatile Dictionary<ComplexityTier, SemaphoreSlim> _semaphores;

    public GenerationService(
        CodeGenerator            codeGenerator,
        IProtocolRepository      protocol,
        ModelConfigService       modelConfig,
        ILogger<GenerationService> logger)
    {
        _codeGenerator = codeGenerator;
        _protocol      = protocol;
        _modelConfig   = modelConfig;
        _logger        = logger;
        _semaphores    = BuildSemaphores(modelConfig.Config);

        modelConfig.ConfigChanged += OnConfigChanged;
    }

    private void OnConfigChanged(ModelConfig cfg)
    {
        Interlocked.Exchange(ref _semaphores, BuildSemaphores(cfg));
        _logger.LogInformation("Semaphores rebuilt after config change");
    }

    /// <summary>
    /// Generates a single packet with throttling and retry.
    /// Never throws (except <see cref="OperationCanceledException"/>).
    /// </summary>
    public async Task<GenerationResult> GenerateAsync(string id, CancellationToken ct)
    {
        var tier = ClassifyTier(id);
        var sem  = _semaphores[tier];

        _logger.LogDebug("[{Id}] Waiting for semaphore (tier={Tier})", id, tier);
        await sem.WaitAsync(ct);
        _logger.LogInformation("[{Id}] Starting generation (tier={Tier})", id, tier);
        try
        {
            return await GenerateWithRetryAsync(id, ct);
        }
        finally
        {
            sem.Release();
            _logger.LogDebug("[{Id}] Semaphore released (tier={Tier})", id, tier);
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
        var idList = ids.ToList();
        _logger.LogInformation("Batch started: {Count} packets", idList.Count);

        var tasks = idList.Select(id => GenerateAsync(id, ct)).ToList();
        await foreach (var task in Task.WhenEach(tasks).WithCancellation(ct))
            yield return await task;

        _logger.LogInformation("Batch finished: {Count} packets", idList.Count);
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
            var sw = Stopwatch.StartNew();
            try
            {
                var data = await _codeGenerator.GenerateAsync(id, ct);
                _logger.LogInformation("[{Id}] OK in {Ms}ms (model={Model})", id, sw.ElapsedMilliseconds, data.Model);
                return GenerationResult.Ok(id, data);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{Id}] Cancelled after {Ms}ms", id, sw.ElapsedMilliseconds);
                throw;
            }
            catch (ClientResultException ex) when (IsRetryable(ex) && attempt < MaxAttempts)
            {
                var body  = ex.GetRawResponse()?.Content.ToString() ?? "";
                var delay = attempt * 2000;
                _logger.LogWarning("[{Id}] HTTP {Status} — retrying in {Delay}ms (attempt {Attempt}/{Max})\nBody: {Body}",
                    id, ex.Status, delay, attempt, MaxAttempts, body);
                await Task.Delay(delay, ct);
            }
            catch (ClientResultException ex)
            {
                var err = MapHttpError(ex);
                _logger.LogError("[{Id}] HTTP {Status} [{Kind}]: {Message}", id, ex.Status, err.Kind, err.Message);
                return GenerationResult.Fail(id, err);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Id}] Unexpected error after {Ms}ms", id, sw.ElapsedMilliseconds);
                return GenerationResult.Fail(id, new GenerationError(GenerationErrorKind.Unknown, ex.Message));
            }
        }

        _logger.LogError("[{Id}] Max retries exceeded", id);
        return GenerationResult.Fail(id, new GenerationError(GenerationErrorKind.Unknown, "Max retries exceeded"));
    }

    private static bool IsRetryable(ClientResultException ex)
    {
        // 429/502/503 — временные проблемы на стороне API, ретраим
        // 400 context_length — контент не изменится, НЕ ретраим
        return ex.Status is 429 or 503 or 502;
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
