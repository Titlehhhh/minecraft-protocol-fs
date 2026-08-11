namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module VehicleMoveServerbound =

    let vehicleMoveServerbound =
        packet "VehicleMovePacket" Play Serverbound All {
            api [
                field "X"        TDouble All
                field "Y"        TDouble All
                field "Z"        TDouble All
                field "Yaw"      TFloat  All
                field "Pitch"    TFloat  All
                field "OnGround" TBool   (Since 769)
            ]

            wire (Until 768) [
                read "x"     F64 "X"
                read "y"     F64 "Y"
                read "z"     F64 "Z"
                read "yaw"   F32 "Yaw"
                read "pitch" F32 "Pitch"
            ]

            wire (Since 769) [
                read "x"        F64  "X"
                read "y"        F64  "Y"
                read "z"        F64  "Z"
                read "yaw"      F32  "Yaw"
                read "pitch"    F32  "Pitch"
                read "onGround" Bool "OnGround"
            ]
        }
