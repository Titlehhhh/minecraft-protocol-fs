namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Vec3f64 =

    let vec3f64 =
        record "Vec3f64" (Since 762) [
            col "x" F64
            col "y" F64
            col "z" F64
        ]
