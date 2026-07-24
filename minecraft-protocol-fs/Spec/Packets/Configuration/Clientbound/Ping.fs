namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PingConfiguration =

    let pingConfiguration =
        packet "PingPacket" Configuration Clientbound (Since 764) {
            api [ field "Id" TInt All ]
            wire (Since 764) [ read "id" I32 "Id" ]
        }
