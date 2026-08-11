namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TransferConfiguration =

    let transferConfiguration =
        packet "TransferPacket" Configuration Clientbound (Since 766) {
            api [
                field "Host" TString All
                field "Port" TInt    All
            ]

            wire (Since 766) [
                read "host" Str    "Host"
                read "port" VarInt "Port"
            ]
        }
