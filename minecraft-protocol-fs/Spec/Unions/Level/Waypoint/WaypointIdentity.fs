namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module WaypointIdentity =

    let waypointIdentity =
        unionType "WaypointIdentity" {
            cases
                (Since 771)
                [
                    case1 1 "Uuid" [ read "uuid" Uuid "Value" ]
                    case1 0 "Id" [ read "id" Str "Value" ]
                ]
        }
