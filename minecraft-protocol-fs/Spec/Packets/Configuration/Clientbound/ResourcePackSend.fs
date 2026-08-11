namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResourcePackSendConfiguration =

    let resourcePackSendConfiguration =
        packet "ResourcePackSendPacket" Configuration Clientbound (Between(764, 764)) {
            api [
                field "Url"           TString           All
                field "Hash"          TString           All
                field "Forced"        TBool             All
                field "PromptMessage" (TOption TString) All
            ]

            wire (Between(764, 764)) [
                read "url"           Str          "Url"
                read "hash"          Str          "Hash"
                read "forced"        Bool         "Forced"
                read "promptMessage" (Option Str) "PromptMessage"
            ]
        }
