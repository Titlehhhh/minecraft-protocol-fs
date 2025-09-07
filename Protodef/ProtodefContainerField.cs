using System.Text.Json.Serialization;

namespace Protodef;

public sealed class ProtodefContainerField
{
    [JsonConstructor]
    public ProtodefContainerField(bool? anon, string? name, ProtodefType type)
    {
        Anon = anon;
        Name = name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    [JsonPropertyName("anon")] public bool? Anon { get; }
    [JsonPropertyName("name")] public string? Name { get; }
    [JsonPropertyName("type")] public ProtodefType Type { get; }

    [JsonIgnore] public bool IsPass { get; set; }
    [JsonIgnore] public bool IsAnon => Anon == true;

    public ProtodefContainerField Clone()
    {
        var clone = new ProtodefContainerField(Anon, Name, (ProtodefType)Type.Clone());
        clone.Type.Parent = null; // или назначишь позже в OnDeserialized
        return clone;
    }
}