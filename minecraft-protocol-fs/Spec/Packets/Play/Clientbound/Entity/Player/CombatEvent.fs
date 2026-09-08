namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CombatEvent =

    let combatEvent =
        packet "CombatEventPacket" Play Clientbound (Until 754) {
            api [ field "Action" (TUnion "CombatEventAction") All ]

            wire (Until 754) [
                read      "event" VarInt "_event"
                readUnion "_event" "CombatEventAction" "Action"
            ]
        }
