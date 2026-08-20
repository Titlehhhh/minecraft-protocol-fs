namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LowDiskSpaceWarning =

    let lowDiskSpaceWarning =
        packet "LowDiskSpaceWarningPacket" Play Clientbound (Since 775) {
            api []
            wire (Since 775) []
        }
