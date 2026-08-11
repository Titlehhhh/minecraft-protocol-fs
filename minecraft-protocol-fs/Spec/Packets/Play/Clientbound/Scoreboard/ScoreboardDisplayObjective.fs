namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ScoreboardDisplayObjective =

    let scoreboardDisplayObjective =
        packet "ScoreboardDisplayObjectivePacket" Play Clientbound All {
            api [
                field "Position" TInt    All
                field "Name"     TString All
            ]

            wire (Until 763) [
                read "position" I8  "Position"
                read "name"     Str "Name"
            ]

            wire (Since 764) [
                read "position" VarInt "Position"
                read "name"     Str    "Name"
            ]
        }
