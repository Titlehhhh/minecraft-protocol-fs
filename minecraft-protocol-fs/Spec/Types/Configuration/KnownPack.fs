namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module KnownPack =

    let knownPack =
        record "KnownPack" (Since 766) [
            col "namespace" Str
            col "id"        Str
            col "version"   Str
        ]
