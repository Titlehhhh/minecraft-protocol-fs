namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ReportDetail =

    let reportDetail =
        record "ReportDetail" (Since 767) [
            col "key"   Str
            col "value" Str
        ]
