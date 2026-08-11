namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TagCategory =

    let tagCategory =
        record "TagCategory" (Since 755) [
            col   "tagType" Str
            colAs "tags"    (Array(Named "Tag", VarIntCount)) "Tags"
        ]
