namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DeathCombatEvent =

    let deathCombatEvent =
        packet "DeathCombatEventPacket" Play Clientbound (Since 755) {
            api [
                field "PlayerId"    TInt    All
                field "EntityId"    TInt    (Between(755, 762))
                field "MessageJson" TString (Between(755, 764))
                field "Message"     TNbt    (Since 765)
            ]

            wire (Between(755, 762)) [
                read "playerId" VarInt "PlayerId"
                read "entityId" I32    "EntityId"
                read "message"  Str    "MessageJson"
            ]

            wire (Between(763, 764)) [
                read "playerId" VarInt "PlayerId"
                read "message"  Str    "MessageJson"
            ]

            wire (Since 765) [
                read "playerId" VarInt  "PlayerId"
                read "message"  AnonNbt "Message"
            ]
        }
