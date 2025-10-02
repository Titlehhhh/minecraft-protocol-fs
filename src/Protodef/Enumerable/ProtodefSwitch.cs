using System.Text.Json.Serialization;

namespace Protodef.Enumerable;

public class ProtodefSwitch : ProtodefType
{
    //TODO path parser
    [JsonPropertyName("compareTo")] public string CompareTo { get; set; }

    [JsonPropertyName("compareToValue")] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompareToValue { get; set; }

    [JsonPropertyName("fields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, ProtodefType>? Fields { get; set; } 

    [JsonPropertyName("default")] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProtodefType? Default { get; set; }

   
    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            if (Fields is null)
                yield break;
            
            foreach (var item in Fields)
                yield return new KeyValuePair<string?, ProtodefType>(item.Key, item.Value);
            if (Default is not null)
                yield return new KeyValuePair<string?, ProtodefType>("default", Default);
        }
    }

    public override bool Equals(object? obj)
    {
         if (ReferenceEquals(this, obj)) return true;
         if (obj is not ProtodefSwitch other) return false;
         return Equals(other);
    }

    private bool EqualsFields(Dictionary<string, ProtodefType>? fields)
    {
        if (fields is null || this.Fields is null) return true;
        
        return this.Fields.SequenceEqual(fields, FieldsEqualityComparer.Instance);
    }
    
    private bool Equals(ProtodefSwitch other)
    {
        return CompareTo == other.CompareTo 
                && EqualsFields(other.Fields) 
                && Equals(Default, other.Default);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CompareTo, CompareToValue, Fields, Default);
    }
    
    
    public override object Clone()
    {
        var owner = new ProtodefSwitch
        {
            CompareToValue = CompareToValue,
            Fields = Fields.Select(x => new KeyValuePair<string, ProtodefType>(x.Key, (ProtodefType)x.Value.Clone()))
                .ToDictionary(),
            Default = Default?.Clone() as ProtodefType
        };

        foreach (var keyValuePair in owner.Fields) keyValuePair.Value.Parent = owner;

        return owner;
    }
    
    private class FieldsEqualityComparer : IEqualityComparer<KeyValuePair<string, ProtodefType>>
    {
        public static readonly FieldsEqualityComparer Instance = new();
        public bool Equals(KeyValuePair<string, ProtodefType> x, KeyValuePair<string, ProtodefType> y)
        {
            return x.Key == y.Key && Equals(x.Value, y.Value);
        }

        public int GetHashCode(KeyValuePair<string, ProtodefType> obj)
        {
            return HashCode.Combine(obj.Key, obj.Value);
        }
    }
    
    
}