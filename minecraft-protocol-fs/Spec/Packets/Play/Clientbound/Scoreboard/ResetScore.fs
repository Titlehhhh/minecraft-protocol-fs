namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ResetScore =

    let resetScore =
        packet "ResetScorePacket" Play Clientbound (Since 765) {
            api [
                field "EntityName"    TString           All
                field "ObjectiveName" (TOption TString) All
            ]

            wire (Since 765) [
                read "entityName"    Str          "EntityName"
                read "objectiveName" (Option Str) "ObjectiveName"
            ]
        }
