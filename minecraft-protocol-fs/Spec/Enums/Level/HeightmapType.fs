namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HeightmapType =

    let heightmapType =
        enumType "HeightmapType" {
            values (Since 771) VarInt [
                0, "world_surface_wg"
                1, "world_surface"
                2, "ocean_floor_wg"
                3, "ocean_floor"
                4, "motion_blocking"
                5, "motion_blocking_no_leaves"
            ]
        }
