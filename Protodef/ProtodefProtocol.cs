using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Protodef;

public class ProtodefProtocol : ProtodefType
{
    public ProtodefProtocol(Dictionary<string, ProtodefType> types, Dictionary<string, ProtodefNamespace> namespaces)
    {
        Types = types;
        Namespaces = namespaces;
    }

    private ProtodefProtocol(ProtodefProtocol other)
    {
        Namespaces = other.Namespaces
            .Select(x => new KeyValuePair<string, ProtodefNamespace>(x.Key, (ProtodefNamespace)x.Value.Clone()))
            .ToDictionary();

        Types = other.Types
            .Select(x => new KeyValuePair<string, ProtodefType>(x.Key, (ProtodefType)x.Value.Clone()))
            .ToDictionary();
    }

    public Dictionary<string, ProtodefType> Types { get; set; }

    public Dictionary<string, ProtodefNamespace> Namespaces { get; }

    public override object Clone()
    {
        return new ProtodefProtocol(this);
    }

    public ProtodefNamespace this[string path]
    {
        get
        {
            var paths = path.Split(".");

            if (paths.Length == 1)
            {
                return Namespaces[path];
            }

            var f = paths.First();


            ProtodefNamespace ns = Namespaces[f];

            for (int i = 1; i < paths.Length; i++)
            {
                string item = paths[i];
                ns = (ProtodefNamespace)ns.Types[item];
            }

            return ns;
        }
    }

    public static ProtodefProtocol Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Dictionary<string, ProtodefType> types = null;
        Dictionary<string, ProtodefNamespace> namespaces = new();

        foreach (var item in root.EnumerateObject())
        {
            if (item.Name == "types")
            {
                types = item.Value.Deserialize<Dictionary<string, ProtodefType>>();
            }
            else
            {
                var namespaceObj = ParseNamespace(item.Value);
                namespaces[item.Name] = namespaceObj;
            }
        }
        var protocol = new ProtodefProtocol(types, namespaces);
        foreach (var item in namespaces)
        {
            item.Value.Parent = protocol;
        }
        foreach (var item in types)
        {
            item.Value.Parent = protocol;
        }

        return protocol;
    }

    private static ProtodefNamespace ParseNamespace(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var types = new Dictionary<string, ProtodefType>();
            foreach (var item in element.EnumerateObject())
            {
                if (item.NameEquals("types"))
                {
                    types = item.Value.Deserialize<Dictionary<string, ProtodefType>>();
                    break;
                }

                var namespaceObj = ParseNamespace(item.Value);
                types[item.Name] = namespaceObj;
            }

            return new ProtodefNamespace { Types = types };
        }

        throw new JsonException("Invalid namespace format.");
    }
}