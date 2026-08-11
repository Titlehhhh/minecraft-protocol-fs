namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PickItemFromEntity =

    let pickItemFromEntity =
        packet "PickItemFromEntityPacket" Play Serverbound (Since 769) {
            api [
                field "EntityId"    TInt  All
                field "IncludeData" TBool All
            ]

            wire (Since 769) [
                read "entityId"    VarInt "EntityId"
                read "includeData" Bool   "IncludeData"
            ]
        }
