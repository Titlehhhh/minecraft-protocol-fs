using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Protodef.Converters;
using Protodef.Enumerable;
using Protodef.Primitive;

namespace Protodef;

public static class Extensions
{
    public static void DeduplicateTypes(this ProtodefType root)
    {
        Stack<ProtodefType> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var (key, child) in current.Children)
            {
                
                if (child is ProtodefCustomType custom)
                {
                    var resolved = ResolveUpwards(current, custom.Name);

                    if (resolved is null)
                        throw new KeyNotFoundException(
                            $"Custom type '{custom.Name}' not found in parent chain.");

                    if (!ReferenceEquals(child, resolved))
                    {
                       
                        if (!current.TryReplaceChild(key, child, resolved))
                            throw new InvalidOperationException(
                                $"Failed to replace type '{custom.Name}' in parent '{current.GetType().Name}'");
                    }

                   
                    stack.Push(resolved);
                }
                else
                {
                    stack.Push(child);
                }
            }
        }
    }

    private static ProtodefType? ResolveUpwards(ProtodefType start, string name)
    {
        ProtodefType? node = start;

        while (node is not null)
        {
            if (node is ProtodefProtocol p)
            {
                if (p.TryFindType(name, out var found))
                    return found;
            }

            if (node is ProtodefNamespace ns)
            {
                if (ns.Types.TryGetValue(name, out var found))
                    return found;
            }

            node = node.Parent;
        }

        return null;
    }

    /// <summary>
    /// Resolves a custom type alias by following chains until a non-custom type is found.
    /// Returns null if the chain cannot be resolved or leads to a cycle.
    /// </summary>
    private static ProtodefType? ResolveAlias(ProtodefType start, string name)
    {
        var resolved = ResolveUpwards(start, name);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

        while (resolved is ProtodefCustomType alias && !seen.Contains(alias.Name))
        {
            seen.Add(alias.Name);
            var next = ResolveUpwards(resolved, alias.Name);
            if (next is null) break;
            resolved = next;
        }

        return resolved;
    }

    private static bool IsDirectPrimitive(ProtodefType t) =>
        t is ProtodefVarInt or ProtodefVarLong or ProtodefBool
            or ProtodefString or ProtodefVoid or ProtodefNumericType
            or ProtodefPrefixedString;

    /// <summary>
    /// Walks the type tree and replaces ProtodefCustomType aliases that resolve to
    /// simple primitives (VarInt, u8, string, etc.) with the actual primitive type.
    /// Complex named types (Slot, GameProfile, etc.) are left as ProtodefCustomType.
    /// </summary>
    private static void ResolvePrimitiveAliasesInPlace(ProtodefType root)
    {
        var stack = new Stack<ProtodefType>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var (key, child) in current.Children.ToList())
            {
                if (child is ProtodefCustomType custom)
                {
                    var resolved = ResolveAlias(current, custom.Name);
                    if (resolved is not null && IsDirectPrimitive(resolved))
                    {
                        var primitiveClone = (ProtodefType)resolved.Clone();
                        current.TryReplaceChild(key, child, primitiveClone);
                        // primitives have no meaningful children
                    }
                    // else: complex named type — leave as ProtodefCustomType
                }
                else
                {
                    stack.Push(child);
                }
            }
        }
    }

    /// <param name="type"></param>
    extension(ProtodefType type)
    {
        public string ToJson()
        {
            return JsonSerializer.Serialize(type, ProtodefType.DefaultJsonOptions);
        }
        
        public ProtodefType CreateDeduplicatedCopy()
        {
            if (type.Clone() is not ProtodefType clone)
                throw new InvalidOperationException("Clone returned incorrect type");



            clone.Parent = type.Parent;
            clone.DeduplicateTypes();
            clone.Parent = null;
            clone.FixParentsRecursive();

            return clone;
        }

        /// <summary>
        /// Creates a deep clone with primitive alias types resolved to their real primitives.
        /// Complex named types (Slot, GameProfile, etc.) remain as ProtodefCustomType.
        /// </summary>
        public ProtodefType CreatePrimitiveResolvedCopy()
        {
            if (type.Clone() is not ProtodefType clone)
                throw new InvalidOperationException("Clone returned incorrect type");

            clone.Parent = type.Parent;
            ResolvePrimitiveAliasesInPlace(clone);
            clone.Parent = null;
            clone.FixParentsRecursive();

            return clone;
        }

        public void FixParentsRecursive()
        {
            foreach (var (_, child) in type.Children)
            {
                child.Parent = type;
                FixParentsRecursive(child);
            }
        }

        /// <summary>
        ///     Checks if the type is a variable long.
        /// </summary>
        /// <returns></returns>
        public bool IsVarLong()
        {
            return type is ProtodefVarLong;
        }

        /// <summary>
        ///     Checks if the type is a variable integer.
        /// </summary>
        /// <returns></returns>
        public bool IsVarInt()
        {
            return type is ProtodefVarInt;
        }

        /// <summary>
        ///     Checks if the type is a variable number.
        /// </summary>
        /// <returns></returns>
        public bool IsVariableNumber()
        {
            return type.IsVarInt() || type.IsVarLong();
        }

        /// <summary>
        ///     Checks if the type is a number.
        /// </summary>
        /// <returns></returns>
        public bool IsNumber()
        {
            return type is ProtodefNumericType || type.IsVariableNumber();
        }

        /// <summary>
        ///     Checks if the type is a boolean.
        /// </summary>
        /// <returns></returns>
        private bool IsBool()
        {
            return type is ProtodefBool;
        }

        /// <summary>
        ///     Checks if the type is a string.
        /// </summary>
        /// <returns></returns>
        private bool IsString()
        {
            return type is ProtodefString;
        }

        /// <summary>
        ///     Checks if the type is void.
        /// </summary>
        /// <returns></returns>
        public bool IsVoid()
        {
            return type is ProtodefVoid;
        }

        /// <summary>
        ///     Checks if the type is custom.
        /// </summary>
        /// <returns></returns>
        public bool IsCustom()
        {
            return type is ProtodefCustomType;
        }

        /// <summary>
        ///     Checks if the type is custom with a specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsCustom(string name)
        {
            return IsCustom(type, [name]);
        }

        /// <summary>
        ///     Checks if the type is custom with a specified name.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public bool IsCustom(IEnumerable<string> names)
        {
            return type is ProtodefCustomType custom && names.Contains(custom.Name);
        }

        /// <summary>
        ///     Checks if the type is a custom switch.
        /// </summary>
        /// <returns></returns>
        public bool IsCustomSwitch()
        {
            return type is ProtodefCustomSwitch;
        }

        /// <summary>
        ///     Checks if the type is a conditional type.
        /// </summary>
        /// <returns></returns>
        public bool IsConditional()
        {
            return type is ProtodefSwitch or ProtodefOption;
        }

        /// <summary>
        ///     Checks if the type is a structure type.
        /// </summary>
        /// <returns></returns>
        public bool IsStructure()
        {
            return type is ProtodefArray or ProtodefContainer;
        }

        /// <summary>
        ///     Checks if the type is a primitive type.
        /// </summary>
        /// <returns></returns>
        public bool IsPrimitive()
        {
            return IsBool(type)
                   || IsString(type)
                   || IsVoid(type);
        }

        /// <summary>
        ///     Checks if the type is a simple type.
        /// </summary>
        /// <returns></returns>
        public bool IsSimple()
        {
            return type.IsPrimitive() ||
                   type.IsNumber() ||
                   type is ProtodefPrefixedString ||
                   type.IsSimpleOption() ||
                   type.IsSimpleArray();
        }

        /// <summary>
        ///     Checks if the type is a simple option type.
        /// </summary>
        /// <returns></returns>
        public bool IsSimpleOption()
        {
            if (type is ProtodefOption option) return option.Type.IsSimple();

            return false;
        }

        /// <summary>
        ///     Checks if the type is a simple array type.
        /// </summary>
        /// <returns></returns>
        public bool IsSimpleArray()
        {
            if (type is ProtodefArray arr) return arr.Type.IsSimple();
            return false;
        }

        /// <summary>
        ///     Checks if the type is a bit field type.
        /// </summary>
        /// <returns></returns>
        public bool IsBitField()
        {
            return type is ProtodefBitField;
        }
        /// <summary>
        ///     Checks if the type is an array type.
        /// </summary>
        /// <returns></returns>
        public bool IsArray()
        {
            return type is ProtodefArray;
        }

        /// <summary>
        ///     Checks if the type is a container type.
        /// </summary>
        /// <returns></returns>
        public bool IsContainer()
        {
            return type is ProtodefContainer;
        }

        /// <summary>
        ///     Checks if the type is a switch type.
        /// </summary>
        /// <returns></returns>
        public bool IsSwitch()
        {
            return type is ProtodefSwitch;
        }

        /// <summary>
        ///     Checks if the type is a buffer type.
        /// </summary>
        /// <returns></returns>
        public bool IsBuffer()
        {
            return type is ProtodefBuffer;
        }

        /// <summary>
        ///     Checks if the type is a mapper type.
        /// </summary>
        /// <returns></returns>
        public bool IsMapper()
        {
            return type is ProtodefMapper;
        }

        /// <summary>
        ///     Checks if the type is a top bit set array type.
        /// </summary>
        /// <returns></returns>
        public bool IsTopBitSetArray()
        {
            if (type is ProtodefTopBitSetTerminatedArray arr)
                if (arr.Type.IsCustom())
                    return true;
            return false;
        }
    }
}