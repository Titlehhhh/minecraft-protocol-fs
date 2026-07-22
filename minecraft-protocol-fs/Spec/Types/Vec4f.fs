namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Vec4f =

    let vec4f =
        record "Vec4f" (Since 762) [
            col "x" F32
            col "y" F32
            col "z" F32
            col "w" F32
        ]
