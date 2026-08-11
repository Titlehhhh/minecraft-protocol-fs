namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomPayloadConfigurationServerbound =

    let customPayloadConfigurationServerbound =
        packet "CustomPayloadPacket" Configuration Serverbound (Since 764) {
            api [
                field "Channel" TString All
                field "Data"    TBytes  All
            ]

            wire (Since 764) [
                read "channel" Str       "Channel"
                read "data"    RestBytes "Data"
            ]
        }
