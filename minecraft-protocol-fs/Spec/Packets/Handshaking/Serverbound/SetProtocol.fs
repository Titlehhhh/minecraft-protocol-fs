namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetProtocol =

    let setProtocol =
        packet "SetProtocolPacket" Handshaking Serverbound All {
            api [
                field "ProtocolVersion" TInt All
                field "ServerHost"      TString All
                field "ServerPort"      TInt All
                field "NextState"       TInt All
            ]

            wire All [
                read "protocolVersion" VarInt "ProtocolVersion"
                read "serverHost"      Str    "ServerHost"
                read "serverPort"      U16    "ServerPort"
                read "nextState"       VarInt "NextState"
            ]
        }
