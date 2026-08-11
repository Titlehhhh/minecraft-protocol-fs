namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetSlotState =

    let setSlotState =
        packet "SetSlotStatePacket" Play Serverbound (Since 765) {
            api [
                field "SlotId"   TInt  All
                field "WindowId" TInt  All
                field "State"    TBool All
            ]

            wire (Since 765) [
                read "slotId"   VarInt "SlotId"
                read "windowId" VarInt "WindowId"
                read "state"    Bool   "State"
            ]
        }
