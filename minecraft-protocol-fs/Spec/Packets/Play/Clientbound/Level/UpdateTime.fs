namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UpdateTime =

    let updateTime =
        packet "UpdateTimePacket" Play Clientbound All {
            api [
                field "Age"         TLong All
                field "Time"        TLong All
                field "TickDayTime" TBool (Since 768)
            ]

            wire (Until 767) [
                read "age"  I64 "Age"
                read "time" I64 "Time"
            ]

            wire (Since 768) [
                read "age"         I64  "Age"
                read "time"        I64  "Time"
                read "tickDayTime" Bool "TickDayTime"
            ]
        }
