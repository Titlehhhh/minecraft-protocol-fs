namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WindowClick =

    let windowClick =
        packet "WindowClickPacket" Play Serverbound All {
            api [
                field "WindowId"     TInt All
                field "StateId"      TInt (Since 756)
                field "Slot"         TInt All
                field "MouseButton"  TInt All
                field "Mode"         TInt All
                field "CursorItem"   (TNamed "Slot") All
            ]

            wire (Until 754) [
                read    "windowId"    U8   "WindowId"
                read    "slot"        I16  "Slot"
                read    "mouseButton" I8   "MouseButton"
                discard "action"      I16
                read    "mode"        I8   "Mode"
                discard "item"        (Named "Slot")
                read    "cursorItem"  (Named "Slot") "CursorItem"
            ]

            wire (Between(755, 755)) [
                read    "windowId"     U8  "WindowId"
                read    "slot"         I16 "Slot"
                read    "mouseButton"  I8  "MouseButton"
                read    "mode"         I8  "Mode"
                discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
                read    "cursorItem"   (Named "Slot") "CursorItem"
            ]

            wire (Between(756, 765)) [
                read    "windowId"     U8     "WindowId"
                read    "stateId"      VarInt "StateId"
                read    "slot"         I16    "Slot"
                read    "mouseButton"  I8     "MouseButton"
                read    "mode"         VarInt "Mode"
                discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
                read    "cursorItem"   (Named "Slot") "CursorItem"
            ]

            wire (Since 770) [
                read    "windowId"     U8     "WindowId"
                read    "stateId"      VarInt "StateId"
                read    "slot"         I16    "Slot"
                read    "mouseButton"  I8     "MouseButton"
                read    "mode"         VarInt "Mode"
                discard "changedSlots" (Array(Named "ChangedSlot", VarIntCount))
                read    "cursorItem"   (Option(Named "HashedSlot")) "CursorItem"
            ]
        }
