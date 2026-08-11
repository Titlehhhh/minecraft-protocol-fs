namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module PackedChunkPos =

    let packedChunkPos =
        record "PackedChunkPos" (Since 762) [
            col "z" I32
            col "x" I32
        ]
