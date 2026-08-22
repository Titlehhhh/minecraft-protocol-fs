namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DifficultyEnum =

    let difficultyEnum =
        enumType "Difficulty" {
            values (Until 770) U8     [ 0, "peaceful"; 1, "easy"; 2, "normal"; 3, "hard" ]
            values (Since 771) VarInt [ 0, "peaceful"; 1, "easy"; 2, "normal"; 3, "hard" ]
        }
