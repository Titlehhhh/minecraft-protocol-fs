namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SelectBundleItem =

    let selectBundleItem =
        packet "SelectBundleItemPacket" Play Serverbound (Since 768) {
            api [
                field "SlotId"            TInt All
                field "SelectedItemIndex" TInt All
            ]

            wire (Since 768) [
                read "slotId"            VarInt "SlotId"
                read "selectedItemIndex" VarInt "SelectedItemIndex"
            ]
        }
