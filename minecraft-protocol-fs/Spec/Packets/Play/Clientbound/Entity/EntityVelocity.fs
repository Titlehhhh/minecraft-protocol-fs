namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityVelocity =

    let entityVelocity =
        packet "EntityVelocityPacket" Play Clientbound All {
            api
                [
                    field "EntityId" TInt All
                    field "VelocityX" TInt (Until 772)
                    field "VelocityY" TInt (Until 772)
                    field "VelocityZ" TInt (Until 772)
                    field "Velocity" (TNamed "LpVec3") (Since 773)
                ]

            wire
                (Until 772)
                [
                    read "entityId" VarInt "EntityId"
                    read "velocityX" I16 "VelocityX"
                    read "velocityY" I16 "VelocityY"
                    read "velocityZ" I16 "VelocityZ"
                ]

            wire
                (Since 773)
                [
                    read "entityId" VarInt "EntityId"
                    read "velocity" (Named "LpVec3") "Velocity"
                ]
        }
