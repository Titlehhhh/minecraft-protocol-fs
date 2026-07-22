namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Vec2f =

    let vec2f =
        record "Vec2f" (Since 767) [
            col "x" F32
            col "y" F32
        ]
