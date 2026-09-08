namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WorldBorder =

    let worldBorder =
        packet "WorldBorderPacket" Play Clientbound (Until 754) {
            api [ field "Action" (TUnion "WorldBorderAction") All ]

            wire (Until 754) [
                read      "action" VarInt "_action"
                readUnion "_action" "WorldBorderAction" "Action"
            ]
        }
