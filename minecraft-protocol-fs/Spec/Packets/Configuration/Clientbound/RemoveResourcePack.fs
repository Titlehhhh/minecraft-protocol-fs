namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RemoveResourcePackConfiguration =

    let removeResourcePackConfiguration =
        packet "RemoveResourcePackPacket" Configuration Clientbound (Since 765) {
            api [
                field "Uuid" (TOption TUuid) All
            ]

            wire (Since 765) [
                read "uuid" (Option Uuid) "Uuid"
            ]
        }
