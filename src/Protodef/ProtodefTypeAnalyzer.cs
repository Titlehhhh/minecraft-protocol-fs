using Protodef.Enumerable;
using Protodef.Primitive;

namespace Protodef;

/// <summary>
/// Analyzes a <see cref="ProtodefType"/> tree and extracts structural information.
/// </summary>
public static class ProtodefTypeAnalyzer
{
    /// <summary>
    /// Returns the set of distinct protodef type kinds used in the given type tree.
    /// Traverses the full tree recursively.
    /// </summary>
    /// <example>
    /// For a container with a varint field and an array of strings, returns:
    /// <c>{ "container", "varint", "array", "string" }</c>
    /// </example>
    public static IReadOnlySet<string> GetTypeComposition(ProtodefType root)
    {
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        Collect(root, kinds);
        return kinds;
    }

    private static void Collect(ProtodefType type, HashSet<string> kinds)
    {
        kinds.Add(GetKindName(type));
        foreach (var (_, child) in type.Children)
            Collect(child, kinds);
    }

    /// <summary>Returns the protodef type identifier string for a given node.</summary>
    public static string GetKindName(ProtodefType type) => type switch
    {
        ProtodefVarInt                   => "varint",
        ProtodefVarLong                  => "varlong",
        ProtodefBool                     => "bool",
        ProtodefVoid                     => "void",
        ProtodefString                   => "string",
        ProtodefPrefixedString           => "pstring",
        ProtodefNumericType n            => n.ProtodefName,  // "i8", "u16", "li32", …
        ProtodefContainer                => "container",
        ProtodefArray                    => "array",
        ProtodefBuffer                   => "buffer",
        ProtodefCustomSwitch n           => n.Owner ?? "cus_switch",
        ProtodefSwitch                   => "switch",
        ProtodefMapper                   => "mapper",
        ProtodefBitField                 => "bitfield",
        ProtodefBitFlags                 => "bitflags",
        ProtodefOption                   => "option",
        ProtodefLoop                     => "loop",
        ProtodefTopBitSetTerminatedArray => "topBitSetTerminatedArray",
        ProtodefRegistryEntryHolder      => "registryEntryHolder",
        ProtodefRegistryEntryHolderSet   => "registryEntryHolderSet",
        ProtodefCustomType cus           => cus.Name,
        _                                => $"unknownType_{type.GetType().Name}"
    };
}
