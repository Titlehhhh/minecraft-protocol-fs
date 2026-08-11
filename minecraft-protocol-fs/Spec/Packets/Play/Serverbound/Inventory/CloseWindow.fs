namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CloseWindowServerbound =

    let closeWindowServerbound =
        packet "CloseWindowPacket" Play Serverbound All {
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
