namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WaypointColor =

    let waypointColor =
        record "WaypointColor" (Since 771) [
            col "red"   U8
            col "green" U8
            col "blue"  U8
        ]
