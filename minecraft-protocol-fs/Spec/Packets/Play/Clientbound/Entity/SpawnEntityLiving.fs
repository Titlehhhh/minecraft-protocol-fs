namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnEntityLiving =

    let spawnEntityLiving =
        packet "SpawnEntityLivingPacket" Play Clientbound (Until 758) {
            api [
                field "EntityId"   TInt    All
                field "EntityUuid" TUuid   All
                field "Type"       TInt    All
                field "X"          TDouble All
                field "Y"          TDouble All
                field "Z"          TDouble All
                field "Yaw"        TInt    All
                field "Pitch"      TInt    All
                field "HeadPitch"  TInt    All
                field "VelocityX"  TInt    All
                field "VelocityY"  TInt    All
                field "VelocityZ"  TInt    All
            ]

            wire (Until 758) [
                read "entityId"   VarInt "EntityId"
                read "entityUUID" Uuid   "EntityUuid"
                read "type"       VarInt "Type"
                read "x"          F64    "X"
                read "y"          F64    "Y"
                read "z"          F64    "Z"
                read "yaw"        I8     "Yaw"
                read "pitch"      I8     "Pitch"
                read "headPitch"  I8     "HeadPitch"
                read "velocityX"  I16    "VelocityX"
                read "velocityY"  I16    "VelocityY"
                read "velocityZ"  I16    "VelocityZ"
            ]
        }
