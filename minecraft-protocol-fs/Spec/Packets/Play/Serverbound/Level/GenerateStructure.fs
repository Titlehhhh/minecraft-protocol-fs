namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GenerateStructure =

    let generateStructure =
        packet "GenerateStructurePacket" Play Serverbound All {
            api [
                field "Location"    (TNamed "Position") All
                field "Levels"      TInt                All
                field "KeepJigsaws" TBool               All
            ]

            wire All [
                read "location"    (Named "Position") "Location"
                read "levels"      VarInt             "Levels"
                read "keepJigsaws" Bool               "KeepJigsaws"
            ]
        }
