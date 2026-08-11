namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderWarningReach =

    let worldBorderWarningReach =
        packet "WorldBorderWarningReachPacket" Play Clientbound (Since 755) {
            api [
                field "WarningBlocks" TInt All
            ]

            wire (Since 755) [
                read "warningBlocks" VarInt "WarningBlocks"
            ]
        }
