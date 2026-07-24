namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KeepAliveConfigurationClientbound =

    let keepAliveConfigurationClientbound =
        packet "KeepAlivePacket" Configuration Clientbound (Since 764) {
            api [ field "KeepAliveId" TLong All ]
            wire (Since 764) [ read "keepAliveId" I64 "KeepAliveId" ]
        }
