namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TeamFlags =

    let teamFlags =
        bitflags "TeamFlags" {
            layout (Since 771) U8 [ "friendlyFire"; "seeFriendlyInvisible" ]
        }
