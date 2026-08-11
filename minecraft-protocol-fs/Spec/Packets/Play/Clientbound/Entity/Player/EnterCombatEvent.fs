namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EnterCombatEvent =

    let enterCombatEvent =
        packet "EnterCombatEventPacket" Play Clientbound (Since 755) {
            api []
            wire (Since 755) []
        }
