namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Vec3i =

    let vec3i =
        record "Vec3i" (Since 770) [
            col "x" VarInt
            col "y" VarInt
            col "z" VarInt
        ]
