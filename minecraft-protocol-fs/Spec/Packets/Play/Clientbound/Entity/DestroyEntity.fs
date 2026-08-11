namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DestroyEntity =

    let destroyEntity =
        packet "DestroyEntityPacket" Play Clientbound (Between(755, 755)) {
            api [
                field "EntityId" TInt All
            ]

            wire (Between(755, 755)) [
                read "entityId" VarInt "EntityId"
            ]
        }
