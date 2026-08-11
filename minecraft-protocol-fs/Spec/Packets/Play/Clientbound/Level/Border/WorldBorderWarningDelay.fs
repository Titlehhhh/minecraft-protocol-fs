namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorderWarningDelay =

    let worldBorderWarningDelay =
        packet "WorldBorderWarningDelayPacket" Play Clientbound (Since 755) {
            api [
                field "WarningTime" TInt All
            ]

            wire (Since 755) [
                read "warningTime" VarInt "WarningTime"
            ]
        }
