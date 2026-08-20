namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateTime =

    let updateTime =
        packet "UpdateTimePacket" Play Clientbound All {
            api
                [
                    field "Age" TLong All
                    field "Time" TLong (Until 774)
                    field "TickDayTime" TBool (Between(768, 774))
                    field "ClockUpdates" (TArray(TNamed "ClockUpdate")) (Since 775)
                ]

            wire (Until 767) [ read "age" I64 "Age"; read "time" I64 "Time" ]

            wire
                (Between(768, 774))
                [
                    read "age" I64 "Age"
                    read "time" I64 "Time"
                    read "tickDayTime" Bool "TickDayTime"
                ]

            wire
                (Since 775)
                [
                    read "age" I64 "Age"
                    read "clockUpdates" (Array(Named "ClockUpdate", VarIntCount)) "ClockUpdates"
                ]
        }
