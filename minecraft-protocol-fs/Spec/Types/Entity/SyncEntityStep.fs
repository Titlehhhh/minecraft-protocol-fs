namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SyncEntityStep =

    let syncEntityStep =
        namedType "SyncEntityStep" {
            api [
                field "X"         TDouble All
                field "Y"         TDouble All
                field "Z"         TDouble All
                field "TickOffset" TInt   All
            ]

            wire (Since 777) [
                read "x"          F64    "X"
                read "y"          F64    "Y"
                read "z"          F64    "Z"
                read "tickOffset" VarInt "TickOffset"
            ]
        }
