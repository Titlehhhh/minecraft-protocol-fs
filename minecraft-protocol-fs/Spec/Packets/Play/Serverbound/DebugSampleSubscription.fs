namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DebugSampleSubscription =

    let debugSampleSubscription =
        packet "DebugSampleSubscriptionPacket" Play Serverbound (Between(766, 772)) {
            api [ field "Type" TInt All ]

            wire (Between(766, 772)) [ read "type" VarInt "Type" ]
        }
