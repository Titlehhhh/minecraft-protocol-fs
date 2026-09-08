namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FacePlayer =

    let facePlayer =
        packet "FacePlayerPacket" Play Clientbound All {
            api [
                field "FeetEyes" TInt All
                field "X"        TDouble All
                field "Y"        TDouble All
                field "Z"        TDouble All
                field "Entity"   (TOption(TNamed "FacePlayerEntityTarget")) All
            ]

            wire All [
                read "feet_eyes" VarInt "FeetEyes"
                read "x"          F64 "X"
                read "y"          F64 "Y"
                read "z"          F64 "Z"
                read "isEntity"   (Option(Named "FacePlayerEntityTarget")) "Entity"
            ]
        }
