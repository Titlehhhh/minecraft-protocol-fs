namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayerInputFlags =

    let playerInputFlags =
        bitflags "PlayerInputFlags" {
            layout (Since 768) U8 [ "forward"; "backward"; "left"; "right"; "jump"; "shift"; "sprint" ]
        }
