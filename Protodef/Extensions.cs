using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;
using Protodef.Converters;
using Protodef.Enumerable;
using Protodef.Primitive;

namespace Protodef;

public static class Extensions
{
    private static readonly HashSet<string> UnknownTypes = new();

    public static void PrintUnknownTypes()
    {
        foreach (var item in UnknownTypes)
        {
            Console.WriteLine(item);
        }
    }
    
    public static bool IsPrimitive(JsonObject jsonObject)
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DataTypeConverter());
        foreach (var (key, value) in jsonObject)
        {
            if (value is JsonArray arr)
            {
                List<ProtodefContainerField> fields = arr.Deserialize<List<ProtodefContainerField>>(options)!;

                ProtodefContainer container = new ProtodefContainer(fields);

                if (container.IsAllFieldsSimple(x =>
                    {
                        bool IsSimpleLocal(ProtodefType type)
                        {
                            if (type.IsBuffer()) return true;

                            if (type is ProtodefCustomType custom)
                            {
                                // true is custom.Name is position, vec2f, vec3f,vec3f64, vec4f, slot, ByteArray, ingredient
                                if (custom.Name is "position"
                                    or "vec2f"
                                    or "vec3f"
                                    or "vec3f64"
                                    or "vec4f"
                                    or "slot"
                                    or "ByteArray"
                                    or "ingredient"
                                    or "UUID"
                                    or "restBuffer"
                                    or "Slot"
                                    or "MovementFlags"
                                    or "PositionUpdateRelatives"
                                    or "optionalNbt"
                                    or "anonymousNbt"
                                    or "nbt")
                                {
                                    return true;
                                }

                                UnknownTypes.Add(custom.Name);
                            }


                            return false;
                        }

                        if (x.Type is ProtodefOption option)
                        {
                            return IsSimpleLocal(option.Type);
                        }

                        if (x.Type is ProtodefArray arr)
                        {
                            return IsSimpleLocal(arr.Type);
                        }

                        return IsSimpleLocal(x.Type);
                    }) == false)
                    return false;
            }
        }

        return true;
    }

    public static bool IsVarLong(this ProtodefType type)
    {
        return type is ProtodefVarLong;
    }

    public static bool IsVarInt(this ProtodefType type)
    {
        return type is ProtodefVarInt;
    }

    public static bool IsVariableNumber(this ProtodefType type)
    {
        return type.IsVarInt() || type.IsVarLong();
    }

    public static bool IsNumber(this ProtodefType type)
    {
        return type is ProtodefNumericType || type.IsVariableNumber();
    }


    private static bool IsBool(this ProtodefType type)
    {
        return type is ProtodefBool;
    }

    private static bool IsString(this ProtodefType type)
    {
        return type is ProtodefString;
    }

    private static bool IsVoid(this ProtodefType type)
    {
        return type is ProtodefVoid;
    }

    public static bool IsCustom(this ProtodefType type)
    {
        return type is ProtodefCustomType;
    }

    public static bool IsCustomSwitch(this ProtodefType type)
    {
        return type is ProtodefCustomSwitch;
    }

    public static bool IsConditional(this ProtodefType type)
    {
        return type is ProtodefSwitch or ProtodefOption;
    }

    public static bool IsStructure(this ProtodefType type)
    {
        return type is ProtodefArray or ProtodefContainer;
    }


    public static bool IsPrimitive(this ProtodefType type)
    {
        return IsBool(type)
               || IsString(type)
               || IsVoid(type);
    }

    public static bool IsSimple(this ProtodefType type)
    {
        return type.IsPrimitive() ||
               type.IsNumber() ||
               type is ProtodefPrefixedString ||
               type.IsSimpleOption() ||
               type.IsSimpleArray();
        //type.IsSimpleSwitch();
    }

    public static bool IsSimpleOption(this ProtodefType type)
    {
        if (type is ProtodefOption option) return option.Type.IsSimple();

        return false;
    }

    public static bool IsSimpleArray(this ProtodefType type)
    {
        if (type is ProtodefArray arr) return arr.Type.IsSimple();
        return false;
    }

    public static bool IsSimpleSwitch(this ProtodefType type)
    {
        if (type is ProtodefSwitch sSwitch)
            if (type is not ProtodefCustomSwitch)
            {
                foreach (var (k, v) in sSwitch.Fields)
                    if (!v.IsSimple())
                        return false;

                if (sSwitch.Default is { } d) return d.IsSimple();

                return true;
            }

        return false;
    }


    public static bool IsAllFieldsSimple(this ProtodefContainer container, Predicate<ProtodefContainerField> custom)
    {
        return container.Fields.All(x =>
        {
            var result = x.Type.IsSimple()
                         || custom(x);

            return result;
        });
    }

    public static void FilterSimple(this ProtodefNamespace ns, Predicate<ProtodefContainerField> custom)
    {
        List<string> keys = new();
        foreach (var (name, type) in ns.Types)
        {
            if (type is ProtodefContainer container)
            {
                if (!container.IsAllFieldsSimple(custom))
                {
                    keys.Add(name);
                }
            }
        }

        keys.ForEach(x => ns.Types.Remove(x));
    }


    public static bool IsBitField(this ProtodefType type)
    {
        return type is ProtodefBitField;
    }

    public static bool IsArray(this ProtodefType type)
    {
        return type is ProtodefArray;
    }

    public static bool IsContainer(this ProtodefType type)
    {
        return type is ProtodefContainer;
    }

    public static bool IsSwitch(this ProtodefType type)
    {
        return type is ProtodefSwitch;
    }

    public static bool IsBuffer(this ProtodefType type)
    {
        return type is ProtodefBuffer;
    }

    public static bool IsMapper(this ProtodefType type)
    {
        return type is ProtodefMapper;
    }

    public static bool IsTopBitSetArray(this ProtodefType type)
    {
        if (type is ProtodefTopBitSetTerminatedArray arr)
            if (arr.Type.IsCustom())
                return true;
        return false;
    }
}