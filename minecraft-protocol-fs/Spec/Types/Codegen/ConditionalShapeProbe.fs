namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ConditionalShapeProbe =

    let conditionalShapeProbe =
        namedType "ConditionalShapeProbe" {
            api
                [
                    field "Kind" TInt All
                    field "Signature" (TOption TBytes) All
                    field "Flag" TInt All
                    field "Block" (TOption(TNamed "Rotations")) All
                ]

            wire
                All
                [
                    read "kind" VarInt "Kind"
                    readOpt "signature" (FixedBytes 4) "Signature" "kind" [ 0 ]
                    read "flag" U8 "Flag"

                    ifNonZero
                        "flag"
                        [
                            readBlock
                                (Named "Rotations")
                                "Block"
                                [ read "pitch" F32 "Pitch"; read "yaw" F32 "Yaw"; read "roll" F32 "Roll" ]
                        ]
                ]
        }
