using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Protodef.Converters;
using Protodef.Enumerable;
using Protodef.Primitive;

namespace Protodef;

public static class Extensions
{
    

    /// <summary>
    ///     Checks if the type is a variable long.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsVarLong(this ProtodefType type)
    {
        return type is ProtodefVarLong;
    }

    /// <summary>
    ///     Checks if the type is a variable integer.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsVarInt(this ProtodefType type)
    {
        return type is ProtodefVarInt;
    }

    /// <summary>
    ///     Checks if the type is a variable number.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsVariableNumber(this ProtodefType type)
    {
        return type.IsVarInt() || type.IsVarLong();
    }

    /// <summary>
    ///     Checks if the type is a number.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNumber(this ProtodefType type)
    {
        return type is ProtodefNumericType || type.IsVariableNumber();
    }


    /// <summary>
    ///     Checks if the type is a boolean.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsBool(this ProtodefType type)
    {
        return type is ProtodefBool;
    }

    /// <summary>
    ///     Checks if the type is a string.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsString(this ProtodefType type)
    {
        return type is ProtodefString;
    }

    /// <summary>
    ///     Checks if the type is void.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsVoid(this ProtodefType type)
    {
        return type is ProtodefVoid;
    }

    /// <summary>
    ///     Checks if the type is custom.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCustom(this ProtodefType type)
    {
        return type is ProtodefCustomType;
    }

    /// <summary>
    ///     Checks if the type is custom with a specified name.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsCustom(this ProtodefType type, string name)
    {
        return type is ProtodefCustomType custom && custom.Name == name;
    }

    /// <summary>
    ///     Checks if the type is custom with a specified name.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="names"></param>
    /// <returns></returns>
    public static bool IsCustom(this ProtodefType type, IEnumerable<string> names)
    {
        return type is ProtodefCustomType custom && names.Contains(custom.Name);
    }

    /// <summary>
    ///     Checks if the type is a custom switch.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCustomSwitch(this ProtodefType type)
    {
        return type is ProtodefCustomSwitch;
    }

    /// <summary>
    ///     Checks if the type is a conditional type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsConditional(this ProtodefType type)
    {
        return type is ProtodefSwitch or ProtodefOption;
    }

    /// <summary>
    ///     Checks if the type is a structure type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsStructure(this ProtodefType type)
    {
        return type is ProtodefArray or ProtodefContainer;
    }


    /// <summary>
    ///     Checks if the type is a primitive type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPrimitive(this ProtodefType type)
    {
        return IsBool(type)
               || IsString(type)
               || IsVoid(type);
    }

    /// <summary>
    ///     Checks if the type is a simple type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimple(this ProtodefType type)
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
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimpleOption(this ProtodefType type)
    {
        if (type is ProtodefOption option) return option.Type.IsSimple();

        return false;
    }

    /// <summary>
    ///     Checks if the type is a simple array type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimpleArray(this ProtodefType type)
    {
        if (type is ProtodefArray arr) return arr.Type.IsSimple();
        return false;
    }

    /// <summary>
    ///     Checks if the type is a bit field type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsBitField(this ProtodefType type)
    {
        return type is ProtodefBitField;
    }

    /// <summary>
    ///     Checks if the type is an array type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsArray(this ProtodefType type)
    {
        return type is ProtodefArray;
    }

    /// <summary>
    ///     Checks if the type is a container type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsContainer(this ProtodefType type)
    {
        return type is ProtodefContainer;
    }

    /// <summary>
    ///     Checks if the type is a switch type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSwitch(this ProtodefType type)
    {
        return type is ProtodefSwitch;
    }

    /// <summary>
    ///     Checks if the type is a buffer type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsBuffer(this ProtodefType type)
    {
        return type is ProtodefBuffer;
    }

    /// <summary>
    ///     Checks if the type is a mapper type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsMapper(this ProtodefType type)
    {
        return type is ProtodefMapper;
    }

    /// <summary>
    ///     Checks if the type is a top bit set array type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsTopBitSetArray(this ProtodefType type)
    {
        if (type is ProtodefTopBitSetTerminatedArray arr)
            if (arr.Type.IsCustom())
                return true;
        return false;
    }
}