namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClockUpdate =

    let clockUpdate =
        record
            "ClockUpdate"
            (Since 775)
            [
                col "id" VarInt
                col "totalTicks" VarLong
                col "partialTick" F32
                col "rate" F32
            ]
