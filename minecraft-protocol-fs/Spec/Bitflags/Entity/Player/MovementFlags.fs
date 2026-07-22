namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MovementFlags =

    let movementFlags =
        bitflags "MovementFlags" {
            layout (Since 768) U8 [ "onGround"; "hasHorizontalCollision" ]
        }
