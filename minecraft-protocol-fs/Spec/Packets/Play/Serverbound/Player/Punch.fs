namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Punch =

    let punch =
        packet "PunchPacket" Play Serverbound (Since 777) {
            api []

            wire (Since 777) []
        }
