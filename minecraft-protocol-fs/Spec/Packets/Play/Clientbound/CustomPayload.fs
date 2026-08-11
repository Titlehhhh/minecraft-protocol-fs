namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomPayloadPlayClientbound =

    let customPayloadPlayClientbound =
        packet "CustomPayloadPacket" Play Clientbound All {
            api [
                field "Channel" TString All
                field "Data"    TBytes  All
            ]

            wire All [
                read "channel" Str       "Channel"
                read "data"    RestBytes "Data"
            ]
        }
