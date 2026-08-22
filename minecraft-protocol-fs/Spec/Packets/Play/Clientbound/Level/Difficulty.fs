namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Difficulty =

    let difficulty =
        packet "DifficultyPacket" Play Clientbound All {
            api [
                field "Difficulty"       (TEnum "Difficulty") All
                field "DifficultyLocked" TBool               All
            ]

            wire (Until 770) [
                read "difficulty"       (enumAs "Difficulty" U8) "Difficulty"
                read "difficultyLocked" Bool                     "DifficultyLocked"
            ]

            wire (Since 771) [
                read "difficulty"       (enumOf "Difficulty") "Difficulty"
                read "difficultyLocked" Bool                  "DifficultyLocked"
            ]
        }
