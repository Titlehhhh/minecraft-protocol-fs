namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TagsConfiguration =

    let tagsConfiguration =
        packet "TagsPacket" Configuration Clientbound (Since 764) {
            api [ field "Tags" (TArray(TNamed "TagCategory")) All ]
            wire (Since 764) [ read "tags" (Array(Named "TagCategory", VarIntCount)) "Tags" ]
        }
