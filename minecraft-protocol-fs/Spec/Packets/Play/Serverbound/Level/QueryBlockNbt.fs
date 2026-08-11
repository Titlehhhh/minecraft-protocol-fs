namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module QueryBlockNbt =

    let queryBlockNbt =
        packet "QueryBlockNbtPacket" Play Serverbound All {
            api [
                field "TransactionId" TInt                All
                field "Location"      (TNamed "Position") All
            ]

            wire All [
                read "transactionId" VarInt             "TransactionId"
                read "location"      (Named "Position") "Location"
            ]
        }
