namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module VillagerData =

    let villagerData =
        namedType "VillagerData" {
            api [
                field "Type"       TInt All
                field "Profession" TInt All
                field "Level"      TInt All
            ]

            wire All [
                read "villagerType"       VarInt "Type"
                read "villagerProfession" VarInt "Profession"
                read "level"              VarInt "Level"
            ]
        }
