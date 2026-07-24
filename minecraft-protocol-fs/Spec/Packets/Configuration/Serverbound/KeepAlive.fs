namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KeepAliveConfigurationServerbound =

    let keepAliveConfigurationServerbound =
        packet "KeepAlivePacket" Configuration Serverbound (Since 764) {
            api [ field "KeepAliveId" TLong All ]
            wire (Since 764) [ read "keepAliveId" I64 "KeepAliveId" ]
        }
