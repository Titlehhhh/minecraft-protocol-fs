namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RemoveEntityEffect =

    let removeEntityEffect =
        packet "RemoveEntityEffectPacket" Play Clientbound All {
            api [
                field "EntityId" TInt All
                field "EffectId" TInt All
            ]

            wire (Until 757) [
                read "entityId" VarInt "EntityId"
                read "effectId" I8     "EffectId"
            ]

            wire (Since 758) [
                read "entityId" VarInt "EntityId"
                read "effectId" VarInt "EffectId"
            ]
        }
