namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TrackedWaypoint =

    let trackedWaypointPacket =
        packet "TrackedWaypointPacket" Play Clientbound (Since 771) {
            api [
                field "Operation" (TEnum "TrackedWaypointOperation") All
                field "Waypoint"  (TNamed "Waypoint") All
            ]

            wire (Since 771) [
                read "operation" (enumOf "TrackedWaypointOperation") "Operation"
                read "waypoint"  (Named "Waypoint") "Waypoint"
            ]
        }
