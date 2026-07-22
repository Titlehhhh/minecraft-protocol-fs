namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MinecartStep =

    let minecartStep =
        record "MinecartStep" (Since 768) [
            col "position" (Named "Vec3f")
            col "movement" (Named "Vec3f")
            col "yaw"      F32
            col "pitch"    F32
            col "weight"   F32
        ]
