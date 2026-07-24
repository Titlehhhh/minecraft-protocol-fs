namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KeepAliveServerbound =

    let keepAliveServerbound =
        packet "KeepAlivePacket" Play Serverbound All {
            api [ field "KeepAliveId" TLong All ]
            wire All [ read "keepAliveId" I64 "KeepAliveId" ]
        }
