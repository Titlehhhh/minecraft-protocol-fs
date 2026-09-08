namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ScoreboardObjective =

    let scoreboardObjective =
        packet "ScoreboardObjectivePacket" Play Clientbound All {
            api [
                field "Name"    TString All
                field "Action"  TInt All
                field "Display" (TOption(TNamed "ObjectiveDisplay")) All
            ]

            wire All [
                read    "name"   Str "Name"
                read    "action" I8  "Action"
                readOpt "display" (Named "ObjectiveDisplay") "Display" "action" [ 0; 2 ]
            ]
        }
