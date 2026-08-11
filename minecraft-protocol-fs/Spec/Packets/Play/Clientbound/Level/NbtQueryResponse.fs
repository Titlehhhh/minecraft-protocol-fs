namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module NbtQueryResponse =

    let nbtQueryResponse =
        packet "NbtQueryResponsePacket" Play Clientbound All {
            api [
                field "TransactionId" TInt           All
                field "Nbt"           (TOption TNbt) All
            ]

            wire (Until 763) [
                read "transactionId" VarInt       "TransactionId"
                read "nbt"           (Option Nbt) "Nbt"
            ]

            wire (Since 764) [
                read "transactionId" VarInt           "TransactionId"
                read "nbt"           (Option AnonNbt) "Nbt"
            ]
        }
