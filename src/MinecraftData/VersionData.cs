using System.Text.Json.Serialization;

namespace MinecraftData;

public class VersionData
{
    [JsonPropertyName("attributes")]
    public string Attributes { get; set; }

    [JsonPropertyName("blocks")]
    public string Blocks { get; set; }

    [JsonPropertyName("blockCollisionShapes")]
    public string BlockCollisionShapes { get; set; }

    [JsonPropertyName("biomes")]
    public string Biomes { get; set; }

    [JsonPropertyName("enchantments")]
    public string Enchantments { get; set; }

    [JsonPropertyName("effects")]
    public string Effects { get; set; }

    [JsonPropertyName("items")]
    public string Items { get; set; }

    [JsonPropertyName("recipes")]
    public string Recipes { get; set; }

    [JsonPropertyName("instruments")]
    public string Instruments { get; set; }

    [JsonPropertyName("materials")]
    public string Materials { get; set; }

    [JsonPropertyName("entities")]
    public string Entities { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; }

    [JsonPropertyName("windows")]
    public string Windows { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("foods")]
    public string Foods { get; set; }

    [JsonPropertyName("particles")]
    public string Particles { get; set; }

    [JsonPropertyName("tints")]
    public string Tints { get; set; }

    [JsonPropertyName("mapIcons")]
    public string MapIcons { get; set; }

    [JsonPropertyName("proto")]
    public string Proto { get; set; }
}