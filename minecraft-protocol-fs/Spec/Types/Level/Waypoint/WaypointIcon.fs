namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WaypointIcon =

    let waypointIcon =
        record "WaypointIcon" (Since 771) [
            col "style" Str
            col "color" (Option(Named "WaypointColor"))
        ]
