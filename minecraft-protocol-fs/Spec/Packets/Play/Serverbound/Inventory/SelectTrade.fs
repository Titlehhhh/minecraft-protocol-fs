namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SelectTrade =

    let selectTrade =
        packet "SelectTradePacket" Play Serverbound All {
            api [
                field "Slot" TInt All
            ]

            wire All [
                read "slot" VarInt "Slot"
            ]
        }
