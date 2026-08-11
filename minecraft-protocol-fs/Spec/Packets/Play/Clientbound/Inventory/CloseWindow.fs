namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CloseWindow =

    let closeWindow =
        packet "CloseWindowPacket" Play Clientbound All {
            api [
                field "WindowId" TInt All
            ]

            wire (Until 767) [
                read "windowId" U8 "WindowId"
            ]

            wire (Since 768) [
                read "windowId" VarInt "WindowId"
            ]
        }
