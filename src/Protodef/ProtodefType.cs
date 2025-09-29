using System.Collections;
using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef;

[JsonConverter(typeof(ProtodefTypeConverter))]
public abstract class ProtodefType : IJsonOnDeserialized, ICloneable
{
    [JsonIgnore]
    public ProtodefType? Parent { get; set; }
    
    [JsonIgnore]
    public string? ParentName { get; set; }


    public abstract object Clone();

    public void OnDeserialized()
    {
        foreach (var item in Children)
        {
            item.Value.Parent = this;
            item.Value.ParentName = item.Key;
        }
    }

    public virtual string? GetNetType()
    {
        return null;
    }


    protected virtual IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl => [];

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public IEnumerable<KeyValuePair<string?, ProtodefType>> Children => ChildrenImpl;
    public static bool operator ==(ProtodefType? a, ProtodefType? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.GetType() != b.GetType()) return false;

        return a.Equals(b);
    }
    
    public static bool operator !=(ProtodefType? a, ProtodefType? b)
    {
        return !(a == b);
    }
}

public sealed class PassableType : ProtodefType
{
    public PassableType(ProtodefType type)
    {
        if (type is PassableType pass)
            Type = pass.Type;
        else
            Type = type;

        //Type.Parent = this;
    }

    public ProtodefType Type { get; set; }

    public override object Clone()
    {
        var g = new PassableType((ProtodefType)this.Type.Clone())
        {
        };

        return g;
    }
}