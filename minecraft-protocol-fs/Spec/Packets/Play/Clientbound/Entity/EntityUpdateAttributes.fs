namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityUpdateAttributes =

    let entityUpdateAttributes =
        packet "EntityUpdateAttributesPacket" Play Clientbound All {
            api [
                field "EntityId"   TInt                               All
                field "Properties" (TArray(TNamed "EntityAttribute")) All
            ]

            wire (Until 754) [
                read "entityId"   VarInt                                           "EntityId"
                read "properties" (Array(Named "EntityAttribute", TypedCount I32)) "Properties"
            ]

            wire (Since 755) [
                read "entityId"   VarInt                                        "EntityId"
                read "properties" (Array(Named "EntityAttribute", VarIntCount)) "Properties"
            ]
        }
