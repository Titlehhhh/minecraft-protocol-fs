using System.Text.Json.Serialization;
using Protodef.Comparers;

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

    [JsonPropertyName("anon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Anon { get; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; }

    [JsonPropertyName("type")] public ProtodefType Type { get; }

    [JsonIgnore] public bool IsPass { get; set; }
    [JsonIgnore] public bool IsAnon => Anon == true;

    public ProtodefContainerField Clone()
    {
        var clone = new ProtodefContainerField(Anon, Name, (ProtodefType)Type.Clone());
        clone.Type.Parent = null; // или назначишь позже в OnDeserialized
        return clone;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ProtodefContainerField other)
        {
            return Equals(this, other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Anon, Name, Type);
    }

    private static bool Equals(ProtodefContainerField? a, ProtodefContainerField? b)
    {
        if (a is null) return false;
        if (b is null) return false;

        return a.Anon == b.Anon && a.Name == b.Name && a.Type.Equals(b.Type);
    }
    
    private static bool EqualsClr(ProtodefContainerField? a, ProtodefContainerField? b)
    {
        if (a is null) return false;
        if (b is null) return false;

        var comparer = ClrTypeComparer.Instance;
        return a.Anon == b.Anon && a.Name == b.Name && comparer.Equals(a.Type, b.Type);
    }

    public static readonly IEqualityComparer<ProtodefContainerField> Comparer =
        EqualityComparer<ProtodefContainerField>.Create(Equals, t=> t.GetHashCode());

    public static readonly IEqualityComparer<ProtodefContainerField> ClrNameComparer =
        EqualityComparer<ProtodefContainerField>.Create(EqualsClr, t =>
        {
            var comparer = ClrTypeComparer.Instance;
            return HashCode.Combine(t.Anon, t.Name, comparer.GetHashCode(t.Type));
        });
}