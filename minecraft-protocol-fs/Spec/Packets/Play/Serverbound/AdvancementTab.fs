namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AdvancementTab =

    let advancementTab =
        packet "AdvancementTabPacket" Play Serverbound All {
            api [
                field "Action" TInt All
                field "TabId"  (TOption TString) All
            ]

            wire All [
                read    "action" VarInt "Action"
                readOpt "tabId" Str "TabId" "action" [ 0 ]
            ]
        }
