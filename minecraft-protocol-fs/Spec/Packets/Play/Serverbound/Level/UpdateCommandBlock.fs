namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateCommandBlock =

    let updateCommandBlock =
        packet "UpdateCommandBlockPacket" Play Serverbound All {
            api [
                field "Location" (TNamed "Position") All
                field "Command"  TString             All
                field "Mode"     TInt                All
                field "Flags"    TInt                All
            ]

            wire All [
                read "location" (Named "Position") "Location"
                read "command"  Str                "Command"
                read "mode"     VarInt             "Mode"
                read "flags"    U8                 "Flags"
            ]
        }
