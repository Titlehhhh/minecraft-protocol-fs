namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ObjectiveDisplay =

    let objectiveDisplay =
        namedType "ObjectiveDisplay" {
            api [
                field "DisplayTextJson" TString (Until 764)
                field "DisplayText"     TNbt (Since 765)
                field "Type"            TInt All
                field "NumberFormat"    (TOption TInt) (Since 765)
                field "Styling"         (TOption TNbt) (Since 765)
            ]

            wire (Until 764) [
                read "displayText" Str    "DisplayTextJson"
                read "type"        VarInt "Type"
            ]

            wire (Since 765) [
                read    "displayText"    AnonNbt "DisplayText"
                read    "type"           VarInt "Type"
                read    "number_format"  (Option VarInt) "NumberFormat"
                readOpt "styling" AnonNbt "Styling" "number_format" [ 1; 2 ]
            ]
        }
