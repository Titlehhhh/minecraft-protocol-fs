namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetDifficulty =

    let setDifficulty =
        packet "SetDifficultyPacket" Play Serverbound All {
            api [
                field "NewDifficulty" (TEnum "Difficulty") All
            ]

            wire (Until 770) [
                read "newDifficulty" (enumAs "Difficulty" U8) "NewDifficulty"
            ]

            wire (Since 771) [
                read "newDifficulty" (enumOf "Difficulty") "NewDifficulty"
            ]
        }
