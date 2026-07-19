namespace McProtocol.Spec

open McProtocol.Dsl

/// Reusable wire shapes that name concrete protocol constructs.
/// These reference specific named types, so they belong to the spec layer,
/// not the generic DSL.
[<AutoOpen>]
module WireAliases =

    let entityMetadataWire =
        SentinelArray(Named "EntityMetadataEntry", 255)
