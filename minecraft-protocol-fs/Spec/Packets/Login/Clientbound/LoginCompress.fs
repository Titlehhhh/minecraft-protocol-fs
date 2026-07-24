namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginCompress =

    let loginCompress =
        packet "LoginCompressPacket" Login Clientbound All {
            protoId "compress"

            api [
                field "Threshold" TInt All
            ]

            wire All [
                read "threshold" VarInt "Threshold"
            ]
        }
