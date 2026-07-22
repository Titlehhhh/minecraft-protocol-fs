namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateViewDistance =

    let updateViewDistance =
        packet "UpdateViewDistancePacket" Play Clientbound All {
            api [
                field "ViewDistance" TInt All
            ]

            wire All [
                read "viewDistance" VarInt "ViewDistance"
            ]
        }
