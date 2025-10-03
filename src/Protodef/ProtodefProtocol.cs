using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Protodef;
using Protodef.Converters;

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

    public ProtodefType FindType(string typeName, IEqualityComparer<string>? comparer = null)
    {
        if (TryFindType(typeName, comparer, out var type)) 
            return type!;
        throw new KeyNotFoundException($"Type '{typeName}' not found");
    }

    protected override IEnumerable<KeyValuePair<string?, ProtodefType>> ChildrenImpl
    {
        get
        {
            foreach (var kv in Types)
                yield return new KeyValuePair<string?, ProtodefType>(kv.Key, kv.Value);
            
            foreach (var kv in Namespaces)
                yield return new KeyValuePair<string?, ProtodefType>(kv.Key, kv.Value);
        }
    }

    public bool TryFindType(string typeName, IEqualityComparer<string>? comparer, out ProtodefType? type)
    {
        if (Types is null)
            throw new InvalidOperationException("Types dictionary is null");
        if (Namespaces is null)
            throw new InvalidOperationException("Namespaces dictionary is null");
        
        var types = GetDict(Types, comparer);

        if (types.TryGetValue(typeName, out type))
            return true;

        foreach (var ns in EnumerateNamespaces())
        {
            types = GetDict(ns.Types, comparer);
            if (types.TryGetValue(typeName, out type))
                return true;
        }

        type = null;
        return false;
    }

    public bool TryFindType(string typeName, out ProtodefType? type)
    {
        return TryFindType(typeName, null, out type);
    }

    private static Dictionary<string, ProtodefType> GetDict(Dictionary<string, ProtodefType> original,
        IEqualityComparer<string>? comparer)
    {
        if (comparer is null)
            return original;
        return new Dictionary<string, ProtodefType>(original, comparer);
    }

    public IEnumerable<ProtodefType> GetAllTypes()
    {
        foreach (var item in Types)
            yield return item.Value;

        foreach (var ns in EnumerateNamespaces())
        {
            foreach (var item in ns.Types)
            {
                if (item.Value is null)
                    throw new InvalidOperationException($"Namespace '{ns.Fullname}' has null type '{item.Key}'");
                yield return item.Value;
            }
        }
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

    private static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions()
    {
        Converters = { new ProtodefTypeConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static ProtodefProtocol Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Dictionary<string, ProtodefType> types = new();
        Dictionary<string, ProtodefNamespace> namespaces = new();

        foreach (var item in root.EnumerateObject())
        {
            if (item.Name == "types")
            {
                foreach (var prop in item.Value.EnumerateObject())
                {
                    try
                    {
                        types[prop.Name] = prop.Value.Deserialize<ProtodefType>(DefaultOptions)!;
                    }
                    catch (Exception e)
                    {
                        throw new JsonException($"Failed to deserialize type '{prop.Name}'", e);
                    }
                }
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
            item.Value.ParentName = item.Key;
            item.Value.Parent = protocol;
            item.Value.OnDeserialized();
        }

        foreach (var item in types)
        {
            item.Value.ParentName = item.Key;
            item.Value.Parent = protocol;
            item.Value.OnDeserialized();
        }

        FixParents(protocol);
        return protocol;
    }

    private static void FixParents(ProtodefType root)
    {
        foreach (var item in root.Children)
        {
            var child = item.Value;
            child.Parent = root;
            child.ParentName = item.Key;
            FixParents(child);
        }
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
                    types = item.Value.Deserialize<Dictionary<string, ProtodefType>>(DefaultOptions);
                    break;
                }

                var namespaceObj = ParseNamespace(item.Value);
                types[item.Name] = namespaceObj;
                namespaceObj.OnDeserialized();
            }

            return new ProtodefNamespace { Types = types };
        }

        throw new JsonException("Invalid namespace format.");
    }

    public IEnumerable<FullnameNamespace> EnumerateNamespaces()
    {
        foreach (var kv in Namespaces)
        {
            string rootName = kv.Key;
            foreach (var ns in EnumerateNamespaces(rootName, kv.Value))
                yield return ns;
        }
    }


    private IEnumerable<FullnameNamespace> EnumerateNamespaces(string prefix, ProtodefNamespace ns)
    {
        foreach (var childKv in ns.Types)
        {
            if (childKv.Value is ProtodefNamespace childNs)
            {
                string childFull = string.IsNullOrEmpty(prefix) ? childKv.Key : $"{prefix}.{childKv.Key}";
                foreach (var sub in EnumerateNamespaces(childFull, childNs))
                    yield return sub;
            }
        }

        var concreteTypes = ns.Types
            .Where(kv => !(kv.Value is ProtodefNamespace))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (concreteTypes.Count > 0)
        {
            yield return new FullnameNamespace(prefix, concreteTypes);
        }
    }
}

public struct FullnameNamespace
{
    public string Fullname { get; }
    public Dictionary<string, ProtodefType?> Types { get; }

    public FullnameNamespace(string fullName, Dictionary<string, ProtodefType> types)
    {
        Fullname = fullName;
        Types = types;
    }
}