namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomPayloadPlayServerbound =

    let customPayloadPlayServerbound =
        packet "CustomPayloadPacket" Play Serverbound All {
            api [
                field "Channel" TString All
                field "Data"    TBytes  All
            ]

            wire All [
                read "channel" Str       "Channel"
                read "data"    RestBytes "Data"
            ]
        }
