namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EntityAttribute =

    let entityAttribute =
        namedType "EntityAttribute" {
            api [
                field "KeyName"   TString                              (Until 765)
                field "KeyId"     TInt                                 (Since 766)
                field "Value"     TDouble                              All
                field "Modifiers" (TArray(TNamed "AttributeModifier")) All
            ]

            wire (Until 754) [
                read "key"       Str                                             "KeyName"
                read "value"     F64                                             "Value"
                read "modifiers" (Array(Named "AttributeModifier", VarIntCount)) "Modifiers"
            ]

            wire (Between(755, 765)) [
                read "name"      Str                                             "KeyName"
                read "value"     F64                                             "Value"
                read "modifiers" (Array(Named "AttributeModifier", VarIntCount)) "Modifiers"
            ]

            wire (Since 766) [
                read "key"       VarInt                                          "KeyId"
                read "value"     F64                                             "Value"
                read "modifiers" (Array(Named "AttributeModifier", VarIntCount)) "Modifiers"
            ]
        }
