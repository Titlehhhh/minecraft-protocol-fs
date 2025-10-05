using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public sealed class ProtodefContainer : ProtodefType
{
    public List<ProtodefContainerField> Fields { get; set; } = new();

    [JsonConstructor]
    public ProtodefContainer(List<ProtodefContainerField> fields)
    {
        Fields = fields;
    }

    public bool Contains(string name) => Fields.Exists(x => x.Name == name);

    private ProtodefContainer(ProtodefContainer other)
    {
        foreach (var field in other.Fields)
        {
            var fieldClone = (ProtodefContainerField)field.Clone();
            //fieldClone.Parent = this;
            Fields.Add(fieldClone);
        }
    }


    public ProtodefType this[string name]
    {
        get
        {
            foreach (var item in Fields)
            {
                if (item.Name == name)
                    return item.Type;
            }

            throw new KeyNotFoundException();
        }
    }

    public override object Clone()
    {
        return new ProtodefContainer(this);
    }

    private bool OrderingEquals(List<ProtodefContainerField> other)
    {
        if (Fields.Count != other.Count)
            return false;
        
        var comparer = FieldEqualityComparer.Instance;
        
        for (int i = 0; i < Fields.Count; i++)
        {
            var f1 = Fields[i];
            var f2 = other[i];
            if (comparer.GetHashCode(f1) != comparer.GetHashCode(f2))
                return false;
            if (!comparer.Equals(f1, f2))
                return false;
        }

        return true;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ProtodefContainer other)
        {
            return false;
        }

        return OrderingEquals(other.Fields);
    }

    private bool Equals(ProtodefContainer other)
    {
        return Fields.Equals(other.Fields);
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl =>
        Fields.Select(f => new KeyValuePair<string?, ProtodefType>(f.Name, f.Type));
    
    private class FieldEqualityComparer : IEqualityComparer<ProtodefContainerField>
    {
        public static readonly IEqualityComparer<ProtodefContainerField> Instance = new FieldEqualityComparer();
        public bool Equals(ProtodefContainerField? x, ProtodefContainerField? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            var a = x.Anon;
            var b = y.Anon;
            var c = x.Type.Equals(y.Type);
            return a == b && x.Name == y.Name && c;
        }

        public int GetHashCode(ProtodefContainerField obj)
        {
            return obj.GetHashCode();
        }
    }
}