namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TrackedWaypointData =

    let trackedWaypointData =
        unionType "TrackedWaypointData" {
            cases
                (Since 771)
                [
                    case1 0 "Empty" []

                    case1 1 "Position" [ read "position" (Named "Vec3i") "Coordinates" ]

                    case1 2 "Chunk" [ read "chunkX" VarInt "ChunkX"; read "chunkZ" VarInt "ChunkZ" ]

                    case1 3 "Azimuth" [ read "azimuth" F32 "Angle" ]
                ]
        }
