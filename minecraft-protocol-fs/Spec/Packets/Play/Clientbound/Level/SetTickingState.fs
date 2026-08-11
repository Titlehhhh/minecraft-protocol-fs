namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetTickingState =

    let setTickingState =
        packet "SetTickingStatePacket" Play Clientbound (Since 765) {
            api [
                field "TickRate" TFloat All
                field "IsFrozen" TBool  All
            ]

            wire (Since 765) [
                read "tickRate" F32  "TickRate"
                read "isFrozen" Bool "IsFrozen"
            ]
        }
