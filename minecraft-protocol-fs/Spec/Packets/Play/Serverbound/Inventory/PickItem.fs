namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PickItem =

    let pickItem =
        packet "PickItemPacket" Play Serverbound (Until 768) {
            api [
                field "Slot" TInt All
            ]

            wire (Until 768) [
                read "slot" VarInt "Slot"
            ]
        }
