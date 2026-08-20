namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SpawnEntity =

    let spawnEntity =
        packet "SpawnEntityPacket" Play Clientbound All {
            api
                [
                    field "EntityId" TInt All
                    field "ObjectUuid" TUuid All
                    field "Type" TInt All
                    field "X" TDouble All
                    field "Y" TDouble All
                    field "Z" TDouble All
                    field "Pitch" TInt All
                    field "Yaw" TInt All
                    field "HeadPitch" TInt (Since 759)
                    field "ObjectData" TInt All
                    field "VelocityX" TInt (Until 772)
                    field "VelocityY" TInt (Until 772)
                    field "VelocityZ" TInt (Until 772)
                    field "Velocity" (TNamed "LpVec3") (Since 773)
                ]

            wire
                (Until 758)
                [
                    read "entityId" VarInt "EntityId"
                    read "objectUUID" Uuid "ObjectUuid"
                    read "type" VarInt "Type"
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "pitch" I8 "Pitch"
                    read "yaw" I8 "Yaw"
                    read "objectData" I32 "ObjectData"
                    read "velocityX" I16 "VelocityX"
                    read "velocityY" I16 "VelocityY"
                    read "velocityZ" I16 "VelocityZ"
                ]

            wire
                (Between(759, 772))
                [
                    read "entityId" VarInt "EntityId"
                    read "objectUUID" Uuid "ObjectUuid"
                    read "type" VarInt "Type"
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "pitch" I8 "Pitch"
                    read "yaw" I8 "Yaw"
                    read "headPitch" I8 "HeadPitch"
                    read "objectData" VarInt "ObjectData"
                    read "velocityX" I16 "VelocityX"
                    read "velocityY" I16 "VelocityY"
                    read "velocityZ" I16 "VelocityZ"
                ]

            wire
                (Since 773)
                [
                    read "entityId" VarInt "EntityId"
                    read "objectUUID" Uuid "ObjectUuid"
                    read "type" VarInt "Type"
                    read "x" F64 "X"
                    read "y" F64 "Y"
                    read "z" F64 "Z"
                    read "velocity" (Named "LpVec3") "Velocity"
                    read "pitch" I8 "Pitch"
                    read "yaw" I8 "Yaw"
                    read "headPitch" I8 "HeadPitch"
                    read "objectData" VarInt "ObjectData"
                ]
        }
