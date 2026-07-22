namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetProjectilePower =

    let setProjectilePower =
        packet "SetProjectilePowerPacket" Play Clientbound (Since 766) {
            api [
                field "Id"                TInt All
                field "Power"             (TNamed "Vec3f64") (Between(766, 766))
                field "AccelerationPower" TDouble (Since 767)
            ]

            wire (Between(766, 766)) [
                read "id"    VarInt "Id"
                read "power" (Named "Vec3f64") "Power"
            ]

            wire (Since 767) [
                read "id"                VarInt "Id"
                read "accelerationPower" F64 "AccelerationPower"
            ]
        }
