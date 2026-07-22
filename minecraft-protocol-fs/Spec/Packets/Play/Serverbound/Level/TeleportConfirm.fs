namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TeleportConfirm =

    let teleportConfirm =
        packet "TeleportConfirmPacket" Play Serverbound All {
            api [
                field "TeleportId" TInt All
            ]

            wire All [
                read "teleportId" VarInt "TeleportId"
            ]
        }
