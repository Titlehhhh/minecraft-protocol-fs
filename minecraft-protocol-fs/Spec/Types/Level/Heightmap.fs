namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Heightmap =

    let heightmap =
        namedType "Heightmap" {
            api [
                field "Type" (TEnum "HeightmapType") (Since 770)
                field "Data" (TArray TLong)          (Since 770)
            ]

            wire (Between(770, 770)) [
                read "type" (enumAs "HeightmapType" VarInt) "Type"
                read "data" (Array(I64, VarIntCount))       "Data"
            ]

            wire (Since 771) [
                read "type" (enumOf "HeightmapType")  "Type"
                read "data" (Array(I64, VarIntCount)) "Data"
            ]
        }
