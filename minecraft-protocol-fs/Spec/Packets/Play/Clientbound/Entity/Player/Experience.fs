namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Experience =

    let experience =
        packet "ExperiencePacket" Play Clientbound All {
            api [
                field "ExperienceBar"   TFloat All
                field "Level"           TInt   All
                field "TotalExperience" TInt   All
            ]

            wire All [
                read "experienceBar"   F32    "ExperienceBar"
                read "level"           VarInt "Level"
                read "totalExperience" VarInt "TotalExperience"
            ]
        }
