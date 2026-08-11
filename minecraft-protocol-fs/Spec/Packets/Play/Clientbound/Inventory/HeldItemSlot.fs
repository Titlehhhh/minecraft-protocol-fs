namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HeldItemSlotClientbound =

    let heldItemSlotClientbound =
        packet "HeldItemSlotPacket" Play Clientbound All {
            api [
                field "Slot" TInt All
            ]

            wire (Until 768) [
                read "slot" I8 "Slot"
            ]

            wire (Since 769) [
                read "slot" VarInt "Slot"
            ]
        }
