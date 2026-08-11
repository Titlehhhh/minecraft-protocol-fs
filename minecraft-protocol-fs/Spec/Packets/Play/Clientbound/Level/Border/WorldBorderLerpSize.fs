namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderLerpSize =

    let worldBorderLerpSize =
        packet "WorldBorderLerpSizePacket" Play Clientbound (Since 755) {
            api [
                field "OldDiameter" TDouble All
                field "NewDiameter" TDouble All
                field "Speed"       TLong   All
            ]

            wire (Between(755, 758)) [
                read "oldDiameter" F64     "OldDiameter"
                read "newDiameter" F64     "NewDiameter"
                read "speed"       VarLong "Speed"
            ]

            wire (Since 759) [
                read "oldDiameter" F64    "OldDiameter"
                read "newDiameter" F64    "NewDiameter"
                read "speed"       VarInt "Speed"
            ]
        }
