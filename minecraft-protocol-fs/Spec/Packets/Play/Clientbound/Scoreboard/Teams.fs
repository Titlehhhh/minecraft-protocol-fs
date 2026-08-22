namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Teams =

    let teamsPacket =
        packet "TeamsPacket" Play Clientbound All {
            api [
                field "TeamName" TString All
                field "Action"   (TUnion "TeamAction") All
            ]

            wire (Until 764) [
                read      "team" Str "TeamName"
                read      "mode" I8  "_mode"
                readUnion "_mode" "TeamAction" "Action"
            ]

            wire (Since 771) [
                read      "team" Str "TeamName"
                read      "mode" I8  "_mode"
                readUnion "_mode" "TeamAction" "Action"
            ]
        }
