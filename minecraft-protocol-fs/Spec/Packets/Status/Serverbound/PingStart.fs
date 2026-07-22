namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingStart =

    let pingStart =
        packet "PingStartPacket" Status Serverbound All {
            api []

            wire All []
        }
