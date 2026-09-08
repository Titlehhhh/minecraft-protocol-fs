namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerLinksResponse =

    let serverLinksResponsePacket =
        packet "ServerLinksResponsePacket" Configuration Serverbound (Between(767, 770)) {
            protoId "server_links"

            api [ field "Links" (TArray(TNamed "ServerLink")) All ]

            wire (Between(767, 770)) [ read "links" (Array(Named "ServerLink", VarIntCount)) "Links" ]
        }
