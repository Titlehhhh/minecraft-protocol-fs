namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TabCompleteServerbound =

    let tabCompleteServerbound =
        packet "TabCompletePacket" Play Serverbound All {
            api [
                field "TransactionId" TInt    All
                field "Text"          TString All
            ]

            wire All [
                read "transactionId" VarInt "TransactionId"
                read "text"          Str    "Text"
            ]
        }
