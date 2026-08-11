namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DebugSampleSubscription =

    let debugSampleSubscription =
        packet "DebugSampleSubscriptionPacket" Play Serverbound (Since 766) {
            api [
                field "Type" TInt All
            ]

            wire (Since 766) [
                read "type" VarInt "Type"
            ]
        }
