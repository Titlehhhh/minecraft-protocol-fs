namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TrackedWaypointOperation =

    let trackedWaypointOperation =
        enumType "TrackedWaypointOperation" { values (Since 771) VarInt [ 0, "track"; 1, "untrack"; 2, "update" ] }
