using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef;

[JsonConverter(typeof(ProtodefTypeConverter))]
public abstract class ProtodefType : IJsonOnDeserialized, ICloneable
{
    public static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        Converters = { new ProtodefTypeConverter() },
        WriteIndented = true
    };
    
    public virtual string ToJson()
    {
        return JsonSerializer.Serialize(this, DefaultJsonOptions);
    }
    
    

    [JsonIgnore] public ProtodefType? Parent { get; set; }

    [JsonIgnore] public string? ParentName { get; set; }


    [JsonIgnore]
    public string Path
    {
        get
        {
            var parts = new List<string>();
            var current = this;

            while (current is not null)
            {
                if (!string.IsNullOrEmpty(current.ParentName))
                    parts.Add(current.ParentName);

                current = current.Parent;
            }

            parts.Reverse();
            return string.Join(".", parts);
        }
    }

    public ProtodefType? GetByPath(string path)
    {
        var parts = path.Split('.');
        var current = this;

        foreach (var part in parts)
        {
            if (current is null) return null;
            current = current.Children.FirstOrDefault(x => x.Key == part).Value;
        }

        return current;
    }
    
    

    public abstract object Clone();

    public void OnDeserialized()
    {
        foreach (var item in Children)
        {
            item.Value.Parent = this;
            item.Value.ParentName = item.Key;
        }
    }

    public virtual string? GetClrType()
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