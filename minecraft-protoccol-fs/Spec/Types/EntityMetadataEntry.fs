namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityMetadataEntry =

    let entityMetadataEntry =
        namedType "EntityMetadataEntry" {
            api [
                field "Index" TInt All
                field "Value" (TUnion "EntityMetadataValue") All
            ]

            wire (Since 766) [
                read      "key"  U8     "Index"
                read      "type" VarInt "_type"
                readUnion "_type" "EntityMetadataValue" "Value"
            ]
        }
