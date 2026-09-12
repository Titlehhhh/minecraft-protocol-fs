namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RegistryEntry =

    let registryEntry =
        record "RegistryEntry" (Since 766) [
            col "key"   Str
            col "value" (Option AnonNbt)
        ]
