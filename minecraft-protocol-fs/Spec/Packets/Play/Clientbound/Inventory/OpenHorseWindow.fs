namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module OpenHorseWindow =

    let openHorseWindow =
        packet "OpenHorseWindowPacket" Play Clientbound All {
            api [
                field "WindowId" TInt All
                field "NbSlots"  TInt All
                field "EntityId" TInt All
            ]

            wire (Until 767) [
                read "windowId" U8     "WindowId"
                read "nbSlots"  VarInt "NbSlots"
                read "entityId" I32    "EntityId"
            ]

            wire (Since 768) [
                read "windowId" VarInt "WindowId"
                read "nbSlots"  VarInt "NbSlots"
                read "entityId" I32    "EntityId"
            ]
        }
