namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RemoveResourcePack =

    let removeResourcePack =
        packet "RemoveResourcePackPacket" Play Clientbound (Since 765) {
            api [
                field "Uuid" (TOption TUuid) All
            ]

            wire (Since 765) [
                read "uuid" (Option Uuid) "Uuid"
            ]
        }
