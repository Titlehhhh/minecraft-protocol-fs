namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ShouldDisplayChatPreview =

    let shouldDisplayChatPreview =
        packet "ShouldDisplayChatPreviewPacket" Play Clientbound (Between(759, 760)) {
            api [
                field "ShouldDisplayChatPreview" TBool All
            ]

            wire (Between(759, 760)) [
                read "shouldDisplayChatPreview" Bool "ShouldDisplayChatPreview"
            ]
        }
