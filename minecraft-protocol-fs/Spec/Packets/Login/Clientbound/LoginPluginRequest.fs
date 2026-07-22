namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginPluginRequest =

    let loginPluginRequest =
        packet "LoginPluginRequestPacket" Login Clientbound All {
            api [
                field "MessageId" TInt    All
                field "Channel"   TString All
                field "Data"      TBytes  All
            ]

            wire All [
                read "messageId" VarInt    "MessageId"
                read "channel"   Str       "Channel"
                read "data"      RestBytes "Data"
            ]
        }
