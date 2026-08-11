namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module OpenBook =

    let openBook =
        packet "OpenBookPacket" Play Clientbound All {
            api [
                field "Hand" TInt All
            ]

            wire All [
                read "hand" VarInt "Hand"
            ]
        }
