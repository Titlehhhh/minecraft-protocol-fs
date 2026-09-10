namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MapChunk =

    let mapChunk =
        packet "MapChunkPacket" Play Clientbound All {
            api [
                field "X"                   TInt                                All
                field "Z"                   TInt                                All
                field "GroundUp"            TInt                                (Until 754)
                field "IgnoreOldData"       TBool                               (Until 736)
                field "BitMapLegacy"        TInt                                (Until 754)
                field "BitMap"              (TArray TLong)                      (Between(755, 756))
                field "HeightmapsLegacy"    TNbt                                (Until 769)
                field "Heightmaps"          (TArray(TNamed "Heightmap"))        (Since 770)
                field "Biomes"              (TOption(TArray TInt))              (Until 756)
                field "ChunkData"           TBytes                              All
                field "BlockEntitiesLegacy" (TArray TNbt)                       (Until 756)
                field "BlockEntities"       (TArray(TNamed "ChunkBlockEntity")) (Since 757)
                field "TrustEdges"          TBool                               (Between(757, 762))
                field "SkyLightMask"        (TArray TLong)                      (Since 757)
                field "BlockLightMask"      (TArray TLong)                      (Since 757)
                field "EmptySkyLightMask"   (TArray TLong)                      (Since 757)
                field "EmptyBlockLightMask" (TArray TLong)                      (Since 757)
                field "SkyLight"            (TArray TBytes)                     (Since 757)
                field "BlockLight"          (TArray TBytes)                     (Since 757)
            ]

            wire (Until 736) [
                read "x"             I32                       "X"
                read "z"             I32                       "Z"
                read "groundUp"      U8                        "GroundUp"
                read "ignoreOldData" Bool                      "IgnoreOldData"
                read "bitMap"        VarInt                    "BitMapLegacy"
                read "heightmaps"    Nbt                       "HeightmapsLegacy"

                ifNonZero "groundUp" [
                    read "biomes" (Array(I32, FixedCount 1024)) "Biomes"
                ]

                read "chunkData"     ByteArray                 "ChunkData"
                read "blockEntities" (Array(Nbt, VarIntCount)) "BlockEntitiesLegacy"
            ]

            wire (Between(751, 754)) [
                read "x"             I32                       "X"
                read "z"             I32                       "Z"
                read "groundUp"      U8                        "GroundUp"
                read "bitMap"        VarInt                    "BitMapLegacy"
                read "heightmaps"    Nbt                       "HeightmapsLegacy"

                ifNonZero "groundUp" [
                    read "biomes" (Array(VarInt, VarIntCount)) "Biomes"
                ]

                read "chunkData"     ByteArray                 "ChunkData"
                read "blockEntities" (Array(Nbt, VarIntCount)) "BlockEntitiesLegacy"
            ]

            wire (Between(755, 756)) [
                read "x"             I32                          "X"
                read "z"             I32                          "Z"
                read "bitMap"        (Array(I64, VarIntCount))    "BitMap"
                read "heightmaps"    Nbt                          "HeightmapsLegacy"
                read "biomes"        (Array(VarInt, VarIntCount)) "Biomes"
                read "chunkData"     ByteArray                    "ChunkData"
                read "blockEntities" (Array(Nbt, VarIntCount))    "BlockEntitiesLegacy"
            ]

            wire (Between(757, 762)) [
                read "x"                   I32                                            "X"
                read "z"                   I32                                            "Z"
                read "heightmaps"          Nbt                                            "HeightmapsLegacy"
                read "chunkData"           ByteArray                                      "ChunkData"
                read "blockEntities"       (Array(Named "ChunkBlockEntity", VarIntCount)) "BlockEntities"
                read "trustEdges"          Bool                                           "TrustEdges"
                read "skyLightMask"        (Array(I64, VarIntCount))                      "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                      "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                      "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                      "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount))   "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount))   "BlockLight"
            ]

            wire (Between(763, 763)) [
                read "x"                   I32                                            "X"
                read "z"                   I32                                            "Z"
                read "heightmaps"          Nbt                                            "HeightmapsLegacy"
                read "chunkData"           ByteArray                                      "ChunkData"
                read "blockEntities"       (Array(Named "ChunkBlockEntity", VarIntCount)) "BlockEntities"
                read "skyLightMask"        (Array(I64, VarIntCount))                      "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                      "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                      "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                      "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount))   "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount))   "BlockLight"
            ]

            wire (Between(764, 769)) [
                read "x"                   I32                                            "X"
                read "z"                   I32                                            "Z"
                read "heightmaps"          AnonNbt                                        "HeightmapsLegacy"
                read "chunkData"           ByteArray                                      "ChunkData"
                read "blockEntities"       (Array(Named "ChunkBlockEntity", VarIntCount)) "BlockEntities"
                read "skyLightMask"        (Array(I64, VarIntCount))                      "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                      "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                      "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                      "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount))   "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount))   "BlockLight"
            ]

            wire (Since 770) [
                read "x"                   I32                                            "X"
                read "z"                   I32                                            "Z"
                read "heightmaps"          (Array(Named "Heightmap", VarIntCount))        "Heightmaps"
                read "chunkData"           ByteArray                                      "ChunkData"
                read "blockEntities"       (Array(Named "ChunkBlockEntity", VarIntCount)) "BlockEntities"
                read "skyLightMask"        (Array(I64, VarIntCount))                      "SkyLightMask"
                read "blockLightMask"      (Array(I64, VarIntCount))                      "BlockLightMask"
                read "emptySkyLightMask"   (Array(I64, VarIntCount))                      "EmptySkyLightMask"
                read "emptyBlockLightMask" (Array(I64, VarIntCount))                      "EmptyBlockLightMask"
                read "skyLight"            (Array(Array(U8, VarIntCount), VarIntCount))   "SkyLight"
                read "blockLight"          (Array(Array(U8, VarIntCount), VarIntCount))   "BlockLight"
            ]
        }
