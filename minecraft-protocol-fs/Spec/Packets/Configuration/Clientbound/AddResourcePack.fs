namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AddResourcePackConfiguration =

    let addResourcePackConfiguration =
        packet "AddResourcePackPacket" Configuration Clientbound (Since 765) {
            api [
                field "Uuid"          TUuid          All
                field "Url"           TString        All
                field "Hash"          TString        All
                field "Forced"        TBool          All
                field "PromptMessage" (TOption TNbt) All
            ]

            wire (Since 765) [
                read "uuid"          Uuid             "Uuid"
                read "url"           Str              "Url"
                read "hash"          Str              "Hash"
                read "forced"        Bool             "Forced"
                read "promptMessage" (Option AnonNbt) "PromptMessage"
            ]
        }
