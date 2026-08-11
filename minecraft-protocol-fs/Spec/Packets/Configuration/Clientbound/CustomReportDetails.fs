namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomReportDetailsConfigurationClientbound =

    let customReportDetailsConfigurationClientbound =
        packet "CustomReportDetailsPacket" Configuration Clientbound (Since 767) {
            api [
                field "Details" (TArray(TNamed "ReportDetail")) All
            ]

            wire (Since 767) [
                read "details" (Array(Named "ReportDetail", VarIntCount)) "Details"
            ]
        }
