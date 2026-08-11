namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CraftProgressBar =

    let craftProgressBar =
        packet "CraftProgressBarPacket" Play Clientbound All {
            api [
                field "WindowId" TInt All
                field "Property" TInt All
                field "Value"    TInt All
            ]

            wire (Until 767) [
                read "windowId" U8  "WindowId"
                read "property" I16 "Property"
                read "value"    I16 "Value"
            ]

            wire (Since 768) [
                read "windowId" VarInt "WindowId"
                read "property" I16    "Property"
                read "value"    I16    "Value"
            ]
        }
