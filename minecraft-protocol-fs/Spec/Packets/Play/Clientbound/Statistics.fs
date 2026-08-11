namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Statistics =

    let statistics =
        packet "StatisticsPacket" Play Clientbound All {
            api [ field "Entries" (TArray(TNamed "StatisticEntry")) All ]
            wire All [ read "entries" (Array(Named "StatisticEntry", VarIntCount)) "Entries" ]
        }
