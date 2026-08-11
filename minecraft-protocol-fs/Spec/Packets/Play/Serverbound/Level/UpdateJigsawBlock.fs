namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateJigsawBlock =

    let updateJigsawBlock =
        packet "UpdateJigsawBlockPacket" Play Serverbound All {
            api [
                field "Location"          (TNamed "Position") All
                field "Name"              TString             All
                field "Target"            TString             All
                field "Pool"              TString             All
                field "FinalState"        TString             All
                field "JointType"         TString             All
                field "SelectionPriority" TInt                (Since 765)
                field "PlacementPriority" TInt                (Since 765)
            ]

            wire (Until 764) [
                read "location"   (Named "Position") "Location"
                read "name"       Str                "Name"
                read "target"     Str                "Target"
                read "pool"       Str                "Pool"
                read "finalState" Str                "FinalState"
                read "jointType"  Str                "JointType"
            ]

            wire (Since 765) [
                read "location"          (Named "Position") "Location"
                read "name"              Str                "Name"
                read "target"            Str                "Target"
                read "pool"              Str                "Pool"
                read "finalState"        Str                "FinalState"
                read "jointType"         Str                "JointType"
                read "selectionPriority" VarInt             "SelectionPriority"
                read "placementPriority" VarInt             "PlacementPriority"
            ]
        }
