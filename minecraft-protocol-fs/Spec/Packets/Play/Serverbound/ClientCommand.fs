namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClientCommand =

    let clientCommand =
        packet "ClientCommandPacket" Play Serverbound All {
            api [
                field "ActionId" TInt All
            ]

            wire All [
                read "actionId" VarInt "ActionId"
            ]
        }
