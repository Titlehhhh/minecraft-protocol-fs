namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Tag =

    let tag =
        record "Tag" All [
            col "tagName" Str
            col "entries" (Array(VarInt, VarIntCount))
        ]
