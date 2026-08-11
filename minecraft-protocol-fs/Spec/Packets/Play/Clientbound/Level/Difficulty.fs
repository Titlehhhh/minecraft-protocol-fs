namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Difficulty =

    let difficulty =
        packet "DifficultyPacket" Play Clientbound All {
            api [
                field "Difficulty"       TInt  All
                field "DifficultyLocked" TBool All
            ]

            wire (Until 770) [
                read "difficulty"       U8   "Difficulty"
                read "difficultyLocked" Bool "DifficultyLocked"
            ]

            wire (Since 771) [
                read "difficulty"       VarInt "Difficulty"
                read "difficultyLocked" Bool   "DifficultyLocked"
            ]
        }
