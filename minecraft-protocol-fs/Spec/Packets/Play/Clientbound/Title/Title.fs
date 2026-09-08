namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Title =

    let title =
        packet "TitlePacket" Play Clientbound (Until 754) {
            api [ field "Action" (TUnion "TitleAction") All ]

            wire (Until 754) [
                read      "action" VarInt "_action"
                readUnion "_action" "TitleAction" "Action"
            ]
        }
