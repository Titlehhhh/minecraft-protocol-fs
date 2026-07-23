using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PacketGenerator.Protocol.Queries;
using PacketGenerator.Protocol.Repository;
using PacketGenerator.Protocol.Serialization;

namespace McpServer.Endpoints;

public static class PacketEndpoints
{
    public static void MapPacketApi(this WebApplication app)
    {
        app.MapGet("/api/packets", (ProtocolQueryService query) =>
        {
            return Results.Ok(query.GetPackets());
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

        app.MapGet("/api/stats", (ProtocolQueryService query) =>
        {
            var stats = query.GetStats();

            return Results.Ok(new
            {
                total = stats.Total,
                tiers = new
                {
                    tiny = stats.Tiers.Tiny,
                    easy = stats.Tiers.Easy,
                    medium = stats.Tiers.Medium,
                    heavy = stats.Tiers.Heavy
                },
                byNamespace = stats.ByNamespace.Select(ns => new
                {
                    ns = ns.Ns,
                    total = ns.Total,
                    tiny = ns.Tiny,
                    easy = ns.Easy,
                    medium = ns.Medium,
                    heavy = ns.Heavy
                }).ToArray(),
                packets = stats.Packets.Select(packet => new { id = packet.Id, score = packet.Score, tier = packet.Tier }).ToArray()
            });
        });

        app.MapGet("/api/types", (ProtocolQueryService query) =>
        {
            return Results.Ok(query.GetTypes());
        });

        app.MapGet("/api/native-types", (ProtocolQueryService query) =>
        {
            return Results.Ok(query.GetNativeTypes());
        });

        app.MapGet("/api/types-by-kind", (ProtocolQueryService query) =>
        {
            return Results.Ok(query.GetTypesByKind());
        });

        app.MapGet("/api/build-order", (ProtocolQueryService query) =>
        {
            try
            {
                return Results.Ok(query.GetBuildOrder());
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/usage", (ProtocolUsageQueries usage, int? top, string? kind) =>
        {
            try
            {
                return Results.Ok(usage.GetUsage(top, kind));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/users/{**id}", (string id, ProtocolUsageQueries usage) =>
        {
            try
            {
                return Results.Ok(usage.GetUsers(id));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/deps/{**id}", (string id, ProtocolUsageQueries usage) =>
        {
            try
            {
                return Results.Ok(usage.GetDependencies(id));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/composition/{**id}", (string id, ProtocolQueryService query) =>
        {
            try
            {
                return Results.Ok(query.GetPacketComposition(id));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/schema/{**id}", (string id, ProtocolQueryService query) =>
        {
            try
            {
                var schema = query.GetPacketSchema(id, OutputFormat.Json);
                return Results.Ok(new
                {
                    json = schema.Json,
                    toon = schema.Toon,
                    complexityScore = schema.ComplexityScore,
                    tier = schema.Tier
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/api/type/{**id}", (string id, ProtocolQueryService query) =>
        {
            try
            {
                var schema = query.GetTypeSchema(id, OutputFormat.Json);
                return Results.Ok(new
                {
                    json = schema.Json,
                    toon = schema.Toon,
                    complexityScore = schema.ComplexityScore,
                    tier = schema.Tier
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });
    }
}
