namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DebugSubscriptionRequest =

    let debugSubscriptionRequest =
        packet "DebugSubscriptionRequestPacket" Play Serverbound (Since 773) {
            api [ field "Subscriptions" (TArray(TEnum "DebugSubscriptionDataType")) All ]

            wire
                (Since 773)
                [ read "subscriptions" (Array(enumOf "DebugSubscriptionDataType", VarIntCount)) "Subscriptions" ]
        }
