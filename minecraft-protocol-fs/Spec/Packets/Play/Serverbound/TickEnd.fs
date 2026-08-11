namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TickEnd =

    let tickEnd =
        packet "TickEndPacket" Play Serverbound (Since 768) {
            api []

            wire (Since 768) []
        }
