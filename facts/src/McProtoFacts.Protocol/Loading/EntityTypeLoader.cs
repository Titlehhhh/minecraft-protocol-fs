using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MinecraftData;
using ProtoCore;
using TruePath.SystemIo;

namespace McProtoFacts.Protocol.Loading;

public sealed record EntityTypeEntry(
    int Id,
    string Name,
    string? DisplayName,
    string? Kind,
    string? Category,
    double? Width,
    double? Height);

public sealed record EntityTypesResult(
    int ProtocolVersion,
    string[] MinecraftVersions,
    string Source,
    int Count,
    EntityTypeEntry[] Entities);

public static class EntityTypeLoader
{
    private const string RegistryFile = "entities.json";
    private const string VersionFileName = "version.json";

    public static async Task<EntityTypesResult> LoadAsync(
        int protocolVersion,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var paths = await DataPathsHelper.GetPCDataPathsAsync();
        var versions = new List<string>();
        var sources = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (minecraftVersion, data) in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(data.Version) || string.IsNullOrEmpty(data.Entities))
                continue;

            var versionPath = MinecraftPaths.DataPath / data.Version / VersionFileName;
            if (!File.Exists(versionPath.Value))
                continue;

            var version = await VersionFile.DeserializeAsync(versionPath);
            if (version.Version != protocolVersion)
                continue;

            versions.Add(minecraftVersion);
            sources.Add(data.Entities);
        }

        if (sources.Count == 0)
            throw new KeyNotFoundException($"No Java data version with protocol {protocolVersion} has an entity type registry.");

        if (sources.Count > 1)
        {
            throw new InvalidOperationException(
                $"Protocol {protocolVersion} maps to more than one entity type registry: {string.Join(", ", sources)}.");
        }

        var source = sources.Min!;
        var registryPath = MinecraftPaths.DataPath / source / RegistryFile;
        var entries = Parse(await registryPath.ReadAllTextAsync(), source);
        var matching = entries
            .Where(e => string.IsNullOrWhiteSpace(filter) || e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        versions.Sort(StringComparer.Ordinal);
        return new EntityTypesResult(protocolVersion, versions.ToArray(), source, matching.Length, matching);
    }

    private static EntityTypeEntry[] Parse(string json, string source)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"The entity type registry of {source} is not a JSON array.");

        var entries = new List<EntityTypeEntry>();
        var seen = new HashSet<int>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (!element.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number
                || !element.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"An entity type in {source} has no numeric id or no name.");
            }

            var entry = new EntityTypeEntry(
                id.GetInt32(),
                name.GetString()!,
                Text(element, "displayName"),
                Text(element, "type"),
                Text(element, "category"),
                Number(element, "width"),
                Number(element, "height"));
            if (!seen.Add(entry.Id))
                throw new InvalidOperationException($"Entity type id {entry.Id} appears twice in {source}.");

            entries.Add(entry);
        }

        entries.Sort((a, b) => a.Id.CompareTo(b.Id));
        return entries.ToArray();
    }

    private static string? Text(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? Number(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
}
