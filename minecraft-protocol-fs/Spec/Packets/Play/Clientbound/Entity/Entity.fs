namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Entity =

    let entity =
        packet "EntityPacket" Play Clientbound (Until 754) {
            api [
                field "EntityId" TInt All
            ]

            wire (Until 754) [
                read "entityId" VarInt "EntityId"
            ]
        }
