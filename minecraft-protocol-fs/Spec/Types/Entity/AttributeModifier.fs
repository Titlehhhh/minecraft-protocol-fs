namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AttributeModifier =

    let attributeModifier =
        namedType "AttributeModifier" {
            api [
                field "Uuid"      TUuid   (Until 766)
                field "Id"        TString (Since 767)
                field "Amount"    TDouble All
                field "Operation" TInt    All
            ]

            wire (Until 766) [
                read "uuid"      Uuid "Uuid"
                read "amount"    F64  "Amount"
                read "operation" I8   "Operation"
            ]

            wire (Since 767) [
                read "uuid"      Str "Id"
                read "amount"    F64 "Amount"
                read "operation" I8  "Operation"
            ]
        }
