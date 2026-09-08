namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Waypoint =

    let waypoint =
        namedType "Waypoint" {
            api [
                field "Identity" (TUnion "WaypointIdentity") All
                field "Icon"     (TNamed "WaypointIcon") All
                field "Data"     (TUnion "TrackedWaypointData") All
            ]

            wire (Since 771) [
                read      "hasUUID" U8     "_hasUUID"
                readUnion "_hasUUID" "WaypointIdentity" "Identity"
                read      "icon" (Named "WaypointIcon") "Icon"
                read      "type"    VarInt "_type"
                readUnion "_type" "TrackedWaypointData" "Data"
            ]
        }
