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
            fieldClone.Parent = this;
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

    public override IEnumerable<KeyValuePair<string?, ProtodefType>> Children =>
        Fields.Select(f => new KeyValuePair<string?, ProtodefType>(f.Name, f.Type));
}