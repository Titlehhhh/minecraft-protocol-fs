using Protodef;

namespace PacketGenerator.Core;

public static class ProtodefPrimitiveExtensions
{
    public static readonly string[] KnownPrimitiveNames =
    [
        "position",
        "vec2f",
        "vec3f",
        "vec3f64",
        "vec4f",
        "slot",
        "ByteArray",
        "ingredient",
        "UUID",
        "restBuffer",
        "Slot",
        "MovementFlags",
        "PositionUpdateRelatives",
        "optionalNbt",
        "anonymousNbt",
        "nbt",
        "anonOptionalNbt",
        "ContainerID"
    ];


    extension(ProtodefType type)
    {
        public bool IsSimpleTypeForGenerator(int maxDepth = -1)
        {
            static bool IsPrimitiveRecursive(ProtodefType pt, int depth)
            {
                if (pt.IsPrimitive())
                    return true;
                if (pt.IsCustom(KnownPrimitiveNames))
                    return true;

                if (depth > 0 && pt.IsContainer())
                    return false;

                return pt.Children
                    .All(x =>
                        IsPrimitiveRecursive(x.Value, depth + 1));
            }

            return IsPrimitiveRecursive(type, 0);
        }
    }
}