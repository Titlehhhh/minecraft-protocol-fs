using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public sealed class ProtodefMapper : ProtodefType
{
    [JsonConstructor]
    public ProtodefMapper(string type, Dictionary<string, string> mappings)
    {
        Type = type;
        Mappings = mappings;
    }

    private ProtodefMapper(ProtodefMapper other)
    {
        Type = other.Type;
        Mappings = new Dictionary<string, string>(other.Mappings);
    }

    [JsonPropertyName("type")] public string Type { get; }

    [JsonPropertyName("mappings")] public Dictionary<string, string> Mappings { get; } = new();

    public override object Clone()
    {
        return new ProtodefMapper(this);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ProtodefMapper other)
        {
            return false;
        }

        return Equals(other);
    }
    private bool EqualsDictionary(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            if (!b.TryGetValue(item.Key, out var value) || value != item.Value) return false;
        }
        return true;
    }
    private bool Equals(ProtodefMapper other)
    {
        return Type == other.Type && EqualsDictionary(Mappings, other.Mappings);
    }
}