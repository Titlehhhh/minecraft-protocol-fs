namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomReportDetails =

    let customReportDetails =
        packet "CustomReportDetailsPacket" Play Clientbound (Since 767) {
            api [ field "Details" (TArray(TNamed "ReportDetail")) All ]
            wire (Since 767) [ read "details" (Array(Named "ReportDetail", VarIntCount)) "Details" ]
        }
