namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LockDifficulty =

    let lockDifficulty =
        packet "LockDifficultyPacket" Play Serverbound All {
            api [
                field "Locked" TBool All
            ]

            wire All [
                read "locked" Bool "Locked"
            ]
        }
