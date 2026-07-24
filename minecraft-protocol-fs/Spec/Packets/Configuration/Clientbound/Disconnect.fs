namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DisconnectConfiguration =

    let disconnectConfiguration =
        packet "DisconnectPacket" Configuration Clientbound (Since 764) {
            api [
                field "ReasonJson" TString (Between(764, 764))
                field "Reason"     TNbt    (Since 765)
            ]
            wire (Between(764, 764)) [ read "reason" Str     "ReasonJson" ]
            wire (Since 765)         [ read "reason" AnonNbt "Reason" ]
        }
