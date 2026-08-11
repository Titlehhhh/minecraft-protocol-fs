namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResourcePackReceiveConfiguration =

    let resourcePackReceiveConfiguration =
        packet "ResourcePackReceivePacket" Configuration Serverbound (Since 764) {
            api [
                field "Uuid"   TUuid (Since 765)
                field "Result" TInt  All
            ]

            wire (Between(764, 764)) [
                read "result" VarInt "Result"
            ]

            wire (Since 765) [
                read "uuid"   Uuid   "Uuid"
                read "result" VarInt "Result"
            ]
        }
