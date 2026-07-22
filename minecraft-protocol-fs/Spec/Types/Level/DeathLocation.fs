namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DeathLocation =

    let deathLocation =
        record "DeathLocation" (Since 759) [
            col "dimensionName" Str
            col "location"      (Named "Position")
        ]
