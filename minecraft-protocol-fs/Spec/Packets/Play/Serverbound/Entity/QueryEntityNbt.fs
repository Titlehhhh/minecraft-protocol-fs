namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module QueryEntityNbt =

    let queryEntityNbt =
        packet "QueryEntityNbtPacket" Play Serverbound All {
            api [
                field "TransactionId" TInt All
                field "EntityId"      TInt All
            ]

            wire All [
                read "transactionId" VarInt "TransactionId"
                read "entityId"      VarInt "EntityId"
            ]
        }
