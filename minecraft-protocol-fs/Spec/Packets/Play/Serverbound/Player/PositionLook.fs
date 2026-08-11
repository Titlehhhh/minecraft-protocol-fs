namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PositionLook =

    let positionLook =
        packet "PositionLookPacket" Play Serverbound All {
            api [
                field "X"        TDouble                  All
                field "Y"        TDouble                  All
                field "Z"        TDouble                  All
                field "Yaw"      TFloat                   All
                field "Pitch"    TFloat                   All
                field "OnGround" TBool                    (Until 767)
                field "Flags"    (TNamed "MovementFlags") (Since 768)
            ]

            wire (Until 767) [
                read "x"        F64  "X"
                read "y"        F64  "Y"
                read "z"        F64  "Z"
                read "yaw"      F32  "Yaw"
                read "pitch"    F32  "Pitch"
                read "onGround" Bool "OnGround"
            ]

            wire (Since 768) [
                read "x"     F64                     "X"
                read "y"     F64                     "Y"
                read "z"     F64                     "Z"
                read "yaw"   F32                     "Yaw"
                read "pitch" F32                     "Pitch"
                read "flags" (Named "MovementFlags") "Flags"
            ]
        }
