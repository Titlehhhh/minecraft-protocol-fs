namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Abilities =

    let abilities =
        packet "AbilitiesPacket" Play Clientbound All {
            api [
                field "Flags"        TInt   All
                field "FlyingSpeed"  TFloat All
                field "WalkingSpeed" TFloat All
            ]

            wire All [
                read "flags"        I8  "Flags"
                read "flyingSpeed"  F32 "FlyingSpeed"
                read "walkingSpeed" F32 "WalkingSpeed"
            ]
        }
