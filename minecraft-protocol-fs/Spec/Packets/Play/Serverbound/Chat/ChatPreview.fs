namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatPreviewServerbound =

    let chatPreviewServerbound =
        packet "ChatPreviewPacket" Play Serverbound (Between(759, 760)) {
            api [
                field "Query"   TInt    All
                field "Message" TString All
            ]

            wire (Between(759, 760)) [
                read "query"   I32 "Query"
                read "message" Str "Message"
            ]
        }
