namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Transaction =

    let transaction =
        packet "TransactionPacket" Play Clientbound (Until 754) {
            api [
                field "WindowId" TInt  All
                field "Action"   TInt  All
                field "Accepted" TBool All
            ]

            wire (Until 754) [
                read "windowId" I8   "WindowId"
                read "action"   I16  "Action"
                read "accepted" Bool "Accepted"
            ]
        }
