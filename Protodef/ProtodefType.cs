using System.Collections;
using System.Text.Json.Serialization;
using Protodef.Converters;

namespace Protodef;

[JsonConverter(typeof(ProtodefTypeConverter))]
public abstract class ProtodefType : IJsonOnDeserialized, ICloneable,
    IEnumerable<KeyValuePair<string?, ProtodefType>>
{
    public ProtodefType? Parent { get; set; }


    public abstract object Clone();

    public void OnDeserialized()
    {
        using var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            item.Value.Parent = this;
        }
    }

    public virtual string? GetNetType()
    {
        return null;
    }

    public virtual IEnumerator<KeyValuePair<string?, ProtodefType>> GetEnumerator()
    {
        return System.Linq.Enumerable.Empty<KeyValuePair<string?, ProtodefType>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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