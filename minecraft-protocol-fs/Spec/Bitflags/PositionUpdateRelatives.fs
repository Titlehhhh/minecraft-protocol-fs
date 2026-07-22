namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PositionUpdateRelatives =

    let positionUpdateRelatives =
        bitflags "PositionUpdateRelatives" {
            layout (Between(766, 767)) U8  [ "x"; "y"; "z"; "yaw"; "pitch" ]
            layout (Since 768)         U32 [ "x"; "y"; "z"; "yaw"; "pitch"; "dx"; "dy"; "dz"; "yawDelta" ]
        }
