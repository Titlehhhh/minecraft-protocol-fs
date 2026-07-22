namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerInfo =

    let serverInfo =
        packet "ServerInfoPacket" Status Clientbound All {
            api [
                field "Response" TString All
            ]

            wire All [
                read "response" Str "Response"
            ]
        }
