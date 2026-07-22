namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Spectate =

    let spectate =
        packet "SpectatePacket" Play Serverbound All {
            api [
                field "Target" TUuid All
            ]

            wire All [
                read "target" Uuid "Target"
            ]
        }
