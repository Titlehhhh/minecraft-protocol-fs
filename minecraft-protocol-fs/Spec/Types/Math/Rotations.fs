namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Rotations =

    let rotations =
        namedType "Rotations" {
            api [
                field "Pitch" TFloat All
                field "Yaw"   TFloat All
                field "Roll"  TFloat All
            ]

            wire All [
                read "pitch" F32 "Pitch"
                read "yaw"   F32 "Yaw"
                read "roll"  F32 "Roll"
            ]
        }
