namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetDifficulty =

    let setDifficulty =
        packet "SetDifficultyPacket" Play Serverbound All {
            api [
                field "NewDifficulty" TInt All
            ]

            wire (Until 770) [
                read "newDifficulty" U8 "NewDifficulty"
            ]

            wire (Since 771) [
                read "newDifficulty" VarInt "NewDifficulty"
            ]
        }
