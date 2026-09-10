namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChunkBlockEntity =

    let chunkBlockEntity =
        namedType "ChunkBlockEntity" {
            api [
                field "PackedXZ" TInt           (Since 757)
                field "Y"        TInt           (Since 757)
                field "Type"     TInt           (Since 757)
                field "NbtData"  (TOption TNbt) (Since 757)
            ]

            wire (Between(757, 763)) [
                read "packedXZ" U8           "PackedXZ"
                read "y"        I16          "Y"
                read "type"     VarInt       "Type"
                read "nbtData"  (Option Nbt) "NbtData"
            ]

            wire (Since 764) [
                read "packedXZ" U8               "PackedXZ"
                read "y"        I16              "Y"
                read "type"     VarInt           "Type"
                read "nbtData"  (Option AnonNbt) "NbtData"
            ]
        }
