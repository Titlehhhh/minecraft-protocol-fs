namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderCenter =

    let worldBorderCenter =
        packet "WorldBorderCenterPacket" Play Clientbound (Since 755) {
            api [
                field "X" TDouble All
                field "Z" TDouble All
            ]

            wire (Since 755) [
                read "x" F64 "X"
                read "z" F64 "Z"
            ]
        }
