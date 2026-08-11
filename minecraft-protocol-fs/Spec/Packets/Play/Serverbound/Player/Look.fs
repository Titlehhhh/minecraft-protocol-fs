namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Look =

    let look =
        packet "LookPacket" Play Serverbound All {
            api [
                field "Yaw"      TFloat                   All
                field "Pitch"    TFloat                   All
                field "OnGround" TBool                    (Until 767)
                field "Flags"    (TNamed "MovementFlags") (Since 768)
            ]

            wire (Until 767) [
                read "yaw"      F32  "Yaw"
                read "pitch"    F32  "Pitch"
                read "onGround" Bool "OnGround"
            ]

            wire (Since 768) [
                read "yaw"   F32                     "Yaw"
                read "pitch" F32                     "Pitch"
                read "flags" (Named "MovementFlags") "Flags"
            ]
        }
