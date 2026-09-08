namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerLinks =

    let serverLinksPacket =
        packet "ServerLinksPacket" Configuration Clientbound (Since 767) {
            api [ field "Links" (TArray(TNamed "ServerLink")) All ]

            wire (Since 767) [ read "links" (Array(Named "ServerLink", VarIntCount)) "Links" ]
        }
