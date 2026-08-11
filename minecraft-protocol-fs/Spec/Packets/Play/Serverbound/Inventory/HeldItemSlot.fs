namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HeldItemSlot =

    let heldItemSlot =
        packet "HeldItemSlotPacket" Play Serverbound All {
            api [
                field "SlotId" TInt All
            ]

            wire All [
                read "slotId" I16 "SlotId"
            ]
        }
