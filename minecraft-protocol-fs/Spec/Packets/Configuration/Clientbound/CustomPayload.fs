namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomPayloadConfigurationClientbound =

    let customPayloadConfigurationClientbound =
        packet "CustomPayloadPacket" Configuration Clientbound (Since 764) {
            api [
                field "Channel" TString All
                field "Data"    TBytes  All
            ]

            wire (Since 764) [
                read "channel" Str       "Channel"
                read "data"    RestBytes "Data"
            ]
        }
