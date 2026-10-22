namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SyncEntityPositionKind =

    let syncEntityPositionKind =
        unionType "SyncEntityPositionKind" {
            cases (Since 777) [
                case1
                    0
                    "Linear"
                    [
                        read "x" F64 "X"
                        read "y" F64 "Y"
                        read "z" F64 "Z"
                    ]

                case1
                    1
                    "Stepped"
                    [ read "steps" (Array(Named "SyncEntityStep", VarIntCount)) "Steps" ]
            ]
        }
