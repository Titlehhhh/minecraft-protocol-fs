namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderSize =

    let worldBorderSize =
        packet "WorldBorderSizePacket" Play Clientbound (Since 755) {
            api [
                field "Diameter" TDouble All
            ]

            wire (Since 755) [
                read "diameter" F64 "Diameter"
            ]
        }
