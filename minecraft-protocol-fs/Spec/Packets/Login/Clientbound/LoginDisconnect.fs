namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginDisconnect =

    let loginDisconnect =
        packet "LoginDisconnectPacket" Login Clientbound All {
            protoId "disconnect"

            api [
                field "Reason" TString All
            ]

            wire All [
                read "reason" Str "Reason"
            ]
        }
