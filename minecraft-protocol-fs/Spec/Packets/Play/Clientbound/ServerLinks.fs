namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PlayServerLinks =

    let playServerLinksPacket =
        packet "PlayServerLinksPacket" Play Clientbound (Since 767) {
            protoId "server_links"

            api [ field "Links" (TArray(TNamed "ServerLink")) All ]

            wire (Since 767) [ read "links" (Array(Named "ServerLink", VarIntCount)) "Links" ]
        }
