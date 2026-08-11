namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TabComplete =

    let tabComplete =
        packet "TabCompletePacket" Play Clientbound All {
            api [
                field "TransactionId" TInt                                All
                field "Start"         TInt                                All
                field "Length"        TInt                                All
                field "Matches"       (TArray(TNamed "TabCompleteMatch")) All
            ]

            wire All [
                read "transactionId" VarInt                                         "TransactionId"
                read "start"         VarInt                                         "Start"
                read "length"        VarInt                                         "Length"
                read "matches"       (Array(Named "TabCompleteMatch", VarIntCount)) "Matches"
            ]
        }
