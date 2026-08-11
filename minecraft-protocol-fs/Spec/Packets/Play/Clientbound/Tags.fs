namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module Tags =

    let tags =
        packet "TagsPacket" Play Clientbound All {
            api [
                field "BlockTags"  (TArray(TNamed "Tag"))         (Until 754)
                field "ItemTags"   (TArray(TNamed "Tag"))         (Until 754)
                field "FluidTags"  (TArray(TNamed "Tag"))         (Until 754)
                field "EntityTags" (TArray(TNamed "Tag"))         (Until 754)
                field "Tags"       (TArray(TNamed "TagCategory")) (Since 755)
            ]

            wire (Until 754) [
                read "blockTags"  (Array(Named "Tag", VarIntCount)) "BlockTags"
                read "itemTags"   (Array(Named "Tag", VarIntCount)) "ItemTags"
                read "fluidTags"  (Array(Named "Tag", VarIntCount)) "FluidTags"
                read "entityTags" (Array(Named "Tag", VarIntCount)) "EntityTags"
            ]

            wire (Since 755) [
                read "tags" (Array(Named "TagCategory", VarIntCount)) "Tags"
            ]
        }
