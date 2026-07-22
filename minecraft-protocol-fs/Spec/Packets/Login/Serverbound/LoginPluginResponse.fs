namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginPluginResponse =

    let loginPluginResponse =
        packet "LoginPluginResponsePacket" Login Serverbound All {
            api [
                field "MessageId" TInt             All
                field "Data"      (TOption TBytes)  All
            ]

            wire All [
                read "messageId" VarInt             "MessageId"
                read "data"      (Option RestBytes)  "Data"
            ]
        }
