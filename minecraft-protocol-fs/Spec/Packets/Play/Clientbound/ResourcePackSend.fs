namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResourcePackSend =

    let resourcePackSend =
        packet "ResourcePackSendPacket" Play Clientbound (Until 764) {
            api [
                field "Url"           TString           All
                field "Hash"          TString           All
                field "Forced"        TBool             (Between(755, 764))
                field "PromptMessage" (TOption TString) (Between(755, 764))
            ]

            wire (Until 754) [
                read "url"  Str "Url"
                read "hash" Str "Hash"
            ]

            wire (Between(755, 764)) [
                read "url"           Str          "Url"
                read "hash"          Str          "Hash"
                read "forced"        Bool         "Forced"
                read "promptMessage" (Option Str) "PromptMessage"
            ]
        }
