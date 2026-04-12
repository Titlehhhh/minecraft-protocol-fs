using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Models;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ProtoCore;

namespace McpServer.Endpoints;

public static class PacketEndpoints
{
    public static void MapPacketApi(this WebApplication app)
    {
        app.MapGet("/api/packets", (IProtocolRepository repo) =>
        {
            var result = repo.GetPackets()
                .ToDictionary(kv => kv.Key, kv => kv.Value.Keys.ToArray());
            return Results.Ok(result);
        });

        app.MapGet("/api/packets/{ns}/{dir}", (string ns, string dir, IProtocolRepository repo) =>
        {
            var key = $"{ns}.{dir}";
            var all = repo.GetPackets();
            if (!all.TryGetValue(key, out var packets))
                return Results.NotFound($"Namespace '{key}' not found.");

            var result = packets.Select(kv => new
            {
                Id        = $"{key}.{kv.Key}",
                kv.Value.Name,
                PacketIds = kv.Value.PacketIds.Select(e => new
                {
                    From  = e.Range.From,
                    To    = e.Range.To,
                    HexId = $"0x{e.Id:X2}"
                }).ToArray()
            }).ToArray();

            return Results.Ok(result);
        });

        app.MapGet("/api/stats", (IProtocolRepository repo, ModelConfigService mcs) =>
        {
            int total = 0, tiny = 0, easy = 0, medium = 0, heavy = 0;
            var byNs      = new SortedDictionary<string, (int Total, int Tiny, int Easy, int Medium, int Heavy)>();
            var perPacket = new List<object>();

            foreach (var (ns, packets) in repo.GetPackets())
            {
                int nsTotal = 0, nsTiny = 0, nsEasy = 0, nsMedium = 0, nsHeavy = 0;
                foreach (var (name, def) in packets)
                {
                    var score     = PacketComplexityScorer.Compute(def.History);
                    var tierEnum  = mcs.ClassifyTier(score);
                    var tierLabel = tierEnum.ToLabel();
                    total++; nsTotal++;
                    switch (tierEnum)
                    {
                        case ComplexityTier.Tiny:   tiny++;   nsTiny++;   break;
                        case ComplexityTier.Easy:   easy++;   nsEasy++;   break;
                        case ComplexityTier.Medium: medium++; nsMedium++; break;
                        default:                    heavy++;  nsHeavy++;  break;
                    }
                    perPacket.Add(new { id = $"{ns}.{name}", score, tier = tierLabel });
                }
                byNs[ns] = (nsTotal, nsTiny, nsEasy, nsMedium, nsHeavy);
            }

            return Results.Ok(new
            {
                total,
                tiers = new { tiny, easy, medium, heavy },
                byNamespace = byNs.Select(kv => new
                {
                    ns     = kv.Key,
                    total  = kv.Value.Total,
                    tiny   = kv.Value.Tiny,
                    easy   = kv.Value.Easy,
                    medium = kv.Value.Medium,
                    heavy  = kv.Value.Heavy
                }).ToArray(),
                packets = perPacket
            });
        });

        app.MapGet("/api/assess/{**id}", async (
            string id,
            IProtocolRepository repo,
            IComplexityAssessor assessor,
            CancellationToken ct) =>
        {
            try
            {
                var packet          = repo.GetPacket(id);
                var structuralScore = PacketComplexityScorer.Compute(packet.History);
                var assessment      = await assessor.AssessAsync(packet.History, ct);
                return Results.Ok(new
                {
                    id,
                    structuralScore,
                    tier     = assessment.Tier.ToLabel(),
                    llmScore = assessment.LlmScore,
                    reason   = assessment.Reason,
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/schema/{**id}", (string id, IProtocolRepository repo, ModelConfigService mcs) =>
        {
            try
            {
                var packet    = repo.GetPacket(id);
                var supported = repo.GetSupportedProtocols();

                var json = System.Text.Json.JsonSerializer.SerializeToNode(
                    packet.History, Protodef.ProtodefType.DefaultJsonOptions)!;
                var obj = json.AsObject();
                PacketPostProcessor.ApplyVersionAliases(obj, supported);

                var jsonStr = System.Text.Json.JsonSerializer.Serialize(json,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var toonStr = Toon.Format.ToonEncoder.EncodeNode(json, new Toon.Format.ToonEncodeOptions());

                var score = PacketComplexityScorer.Compute(packet.History);
                var tier  = mcs.ClassifyTier(score).ToLabel();

                return Results.Ok(new { json = jsonStr, toon = toonStr, complexityScore = score, tier });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });
    }
}
