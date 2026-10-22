namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DebugSubscriptionDataType =

    let debugSubscriptionDataType =
        enumType "DebugSubscriptionDataType" {
            values
                (Since 773)
                VarInt
                [
                    0, "DedicatedServerTickTime"
                    1, "Bees"
                    2, "Brains"
                    3, "Breezes"
                    4, "GoalSelectors"
                    5, "EntityPaths"
                    6, "EntityBlockIntersections"
                    7, "BeeHives"
                    8, "Pois"
                    9, "RedstoneWireOrientations"
                    10, "VillageSections"
                    11, "Raids"
                    12, "Structures"
                    13, "GameEventListeners"
                    14, "NeighborUpdates"
                    15, "GameEvents"
                ]
        }
