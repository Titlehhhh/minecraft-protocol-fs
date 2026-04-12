using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Http;

namespace McpServer.Helpers;

public static class BatchSseStreamer
{
    public static async Task StreamAsync(
        HttpContext http,
        CodeGenerator gen,
        IProtocolRepository proto,
        ModelConfigService mcs,
        string[] ids,
        string? outputBaseDir,
        CancellationToken ct)
    {
        http.Response.ContentType             = "text/event-stream; charset=utf-8";
        http.Response.Headers["Cache-Control"]      = "no-cache";
        http.Response.Headers["X-Accel-Buffering"]  = "no";

        var writeLock = new SemaphoreSlim(1, 1);
        int okCount = 0, errCount = 0;

        async Task WriteSse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            await writeLock.WaitAsync(ct);
            try
            {
                await http.Response.WriteAsync($"data: {json}\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
            }
            finally { writeLock.Release(); }
        }

        await WriteSse(new { type = "start", total = ids.Length });

        var semaphores = BuildTierSemaphores(mcs.Config);
        try
        {
            var tasks = ids.Select(async id =>
            {
                SemaphoreSlim sem;
                try
                {
                    var def  = proto.GetPacket(id);
                    var tier = mcs.ClassifyTier(PacketComplexityScorer.Compute(def.History));
                    sem = semaphores[tier];
                }
                catch { sem = semaphores[ComplexityTier.Easy]; }

                await sem.WaitAsync(ct);
                try
                {
                    string? error = null, model = null, savedTo = null;
                    long elapsedMs = 0;
                    try
                    {
                        var data = await gen.GenerateAsync(id, ct);
                        model = data.Model;
                        elapsedMs = data.ElapsedMs;

                        if (!string.IsNullOrWhiteSpace(outputBaseDir) && !string.IsNullOrWhiteSpace(data.Code))
                        {
                            try
                            {
                                var dir  = Path.Combine(outputBaseDir, PacketFileHelper.ResolveSubdir(id));
                                Directory.CreateDirectory(dir);
                                var path = Path.Combine(dir, data.Name + ".cs");
                                await File.WriteAllTextAsync(path, data.Code, ct);
                                savedTo = path;
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"[Batch] Save failed '{id}': {ex.Message}");
                            }
                        }
                        Interlocked.Increment(ref okCount);
                    }
                    catch (Exception ex)
                    {
                        error = $"{ex.GetType().Name}: {ex.Message}";
                        Interlocked.Increment(ref errCount);
                    }

                    await WriteSse(new { type = "packet", id, success = error == null, model, elapsedMs, savedTo, error });
                }
                finally { sem.Release(); }
            }).ToArray();

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
          try { await WriteSse(new { type = "done", total = ids.Length, ok = okCount, err = errCount }); }
          catch { /* client may have disconnected */ }
          
          foreach (var s in semaphores.Values) s.Dispose();
            writeLock.Dispose();
        }
    }

    private static Dictionary<ComplexityTier, SemaphoreSlim> BuildTierSemaphores(ModelConfig cfg) =>
        Enum.GetValues<ComplexityTier>().ToDictionary(t => t, t => new SemaphoreSlim(Math.Max(1, t switch
        {
            ComplexityTier.Tiny   => cfg.Tiny.MaxConcurrency,
            ComplexityTier.Easy   => cfg.Easy.MaxConcurrency,
            ComplexityTier.Medium => cfg.Medium.MaxConcurrency,
            ComplexityTier.Heavy  => cfg.Heavy.MaxConcurrency,
            _                     => 4,
        })));
}
