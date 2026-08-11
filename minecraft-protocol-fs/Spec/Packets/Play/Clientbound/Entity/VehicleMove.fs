namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module VehicleMove =

    let vehicleMove =
        packet "VehicleMovePacket" Play Clientbound All {
            api [
                field "X"     TDouble All
                field "Y"     TDouble All
                field "Z"     TDouble All
                field "Yaw"   TFloat  All
                field "Pitch" TFloat  All
            ]

            wire All [
                read "x"     F64 "X"
                read "y"     F64 "Y"
                read "z"     F64 "Z"
                read "yaw"   F32 "Yaw"
                read "pitch" F32 "Pitch"
            ]
        }
