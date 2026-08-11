namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatPreview =

    let chatPreview =
        packet "ChatPreviewPacket" Play Clientbound (Between(759, 760)) {
            api [
                field "QueryId" TInt              All
                field "Message" (TOption TString) All
            ]

            wire (Between(759, 760)) [
                read "queryId" I32          "QueryId"
                read "message" (Option Str) "Message"
            ]
        }
