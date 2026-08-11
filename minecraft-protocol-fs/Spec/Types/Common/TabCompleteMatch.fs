namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TabCompleteMatch =

    let tabCompleteMatch =
        namedType "TabCompleteMatch" {
            api [
                field "Match"       TString           All
                field "TooltipJson" (TOption TString) (Until 764)
                field "Tooltip"     (TOption TNbt)    (Since 765)
            ]

            wire (Until 764) [
                read "match"   Str          "Match"
                read "tooltip" (Option Str) "TooltipJson"
            ]

            wire (Since 765) [
                read "match"   Str              "Match"
                read "tooltip" (Option AnonNbt) "Tooltip"
            ]
        }
