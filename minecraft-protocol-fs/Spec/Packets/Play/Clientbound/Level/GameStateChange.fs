namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GameStateChange =

    let gameStateChange =
        packet "GameStateChangePacket" Play Clientbound All {
            api [
                field "Reason"   TInt   All
                field "GameMode" TFloat All
            ]

            wire All [
                read "reason"   U8  "Reason"
                read "gameMode" F32 "GameMode"
            ]
        }
