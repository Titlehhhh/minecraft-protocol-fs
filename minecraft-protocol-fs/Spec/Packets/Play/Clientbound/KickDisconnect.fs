namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KickDisconnect =

    let kickDisconnect =
        packet "KickDisconnectPacket" Play Clientbound All {
            api [
                field "ReasonJson" TString (Until 764)
                field "Reason"     TNbt    (Since 765)
            ]

            wire (Until 764) [ read "reason" Str     "ReasonJson" ]
            wire (Since 765) [ read "reason" AnonNbt "Reason" ]
        }
