namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Vec3f =

    let vec3f =
        record "Vec3f" (Since 762) [
            col "x" F32
            col "y" F32
            col "z" F32
        ]
