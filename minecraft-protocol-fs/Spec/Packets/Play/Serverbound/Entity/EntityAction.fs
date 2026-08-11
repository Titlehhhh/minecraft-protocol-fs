namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityAction =

    let entityAction =
        packet "EntityActionPacket" Play Serverbound All {
            api [
                field "EntityId"  TInt All
                field "ActionId"  TInt All
                field "JumpBoost" TInt All
            ]

            wire All [
                read "entityId"  VarInt "EntityId"
                read "actionId"  VarInt "ActionId"
                read "jumpBoost" VarInt "JumpBoost"
            ]
        }
