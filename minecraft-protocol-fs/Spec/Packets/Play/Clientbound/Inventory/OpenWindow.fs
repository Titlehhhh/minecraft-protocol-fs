namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module OpenWindow =

    let openWindow =
        packet "OpenWindowPacket" Play Clientbound All {
            api [
                field "WindowId"        TInt    All
                field "InventoryType"   TInt    All
                field "WindowTitleJson" TString (Until 764)
                field "WindowTitle"     TNbt    (Since 765)
            ]

            wire (Until 764) [
                read "windowId"      VarInt  "WindowId"
                read "inventoryType" VarInt  "InventoryType"
                read "windowTitle"   Str     "WindowTitleJson"
            ]

            wire (Since 765) [
                read "windowId"      VarInt  "WindowId"
                read "inventoryType" VarInt  "InventoryType"
                read "windowTitle"   AnonNbt "WindowTitle"
            ]
        }
