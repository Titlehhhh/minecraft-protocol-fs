namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module StatisticEntry =

    let statisticEntry =
        record "StatisticEntry" All [
            col "categoryId"  VarInt
            col "statisticId" VarInt
            col "value"       VarInt
        ]
