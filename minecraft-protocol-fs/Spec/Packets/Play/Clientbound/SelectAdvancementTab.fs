namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SelectAdvancementTab =

    let selectAdvancementTab =
        packet "SelectAdvancementTabPacket" Play Clientbound All {
            api [
                field "Id" (TOption TString) All
            ]

            wire All [
                read "id" (Option Str) "Id"
            ]
        }
