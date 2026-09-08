namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ScoreboardScore =

    let scoreboardScore =
        packet "ScoreboardScorePacket" Play Clientbound All {
            api [
                field "EntityName"    TString All
                field "ObjectiveName" TString All
                field "Action"        TInt (Until 764)
                field "Value"         (TOption TInt) All
                field "DisplayName"   (TOption TNbt) (Since 765)
                field "NumberFormat"  (TOption TInt) (Since 765)
                field "Styling"       (TOption TNbt) (Since 765)
            ]

            wire (Until 764) [
                read    "itemName"  Str    "EntityName"
                read    "action"    VarInt "Action"
                read    "scoreName" Str    "ObjectiveName"
                readOpt "value" VarInt "Value" "action" [ 0 ]
            ]

            wire (Since 765) [
                read    "itemName"       Str "EntityName"
                read    "scoreName"      Str "ObjectiveName"
                read    "value"          VarInt "Value"
                read    "display_name"   (Option AnonNbt) "DisplayName"
                read    "number_format"  (Option VarInt) "NumberFormat"
                readOpt "styling" AnonNbt "Styling" "number_format" [ 1; 2 ]
            ]
        }
