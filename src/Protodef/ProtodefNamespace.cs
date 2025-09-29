using System.Collections;

namespace Protodef;

public sealed class ProtodefNamespace : ProtodefType
{
    public Dictionary<string, ProtodefType> Types { get; set; } = new();

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
        => Types.Select(x 
            => new KeyValuePair<string?, ProtodefType>(x.Key, x.Value));

    public override object Clone()
    {
        var owner = new ProtodefNamespace
        {
            Types = Types
                .Select(x => new KeyValuePair<string, ProtodefType>(x.Key, (ProtodefType)x.Value.Clone()))
                .ToDictionary()
        };

        foreach (var item in owner.Types) item.Value.Parent = owner;

        return owner;
    }
}