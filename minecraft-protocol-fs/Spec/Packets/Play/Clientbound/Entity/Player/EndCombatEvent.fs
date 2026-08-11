namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EndCombatEvent =

    let endCombatEvent =
        packet "EndCombatEventPacket" Play Clientbound (Since 755) {
            api [
                field "Duration" TInt All
                field "EntityId" TInt (Between(755, 762))
            ]

            wire (Between(755, 762)) [
                read "duration" VarInt "Duration"
                read "entityId" I32    "EntityId"
            ]

            wire (Since 763) [
                read "duration" VarInt "Duration"
            ]
        }
