namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TestInstanceBlockActionData =

    let testInstanceBlockActionData =
        namedType "TestInstanceBlockActionData" {
            api
                [
                    field "Test"           (TOption TString) All
                    field "Size"           (TNamed "Vec3i") All
                    field "Rotation"       TInt            All
                    field "IgnoreEntities" TBool           All
                    field "Status"         TInt            All
                    field "ErrorMessage"   (TOption TNbt)  All
                ]

            wire
                All
                [
                    read "test"           (Option Str)     "Test"
                    read "size"           (Named "Vec3i")   "Size"
                    read "rotation"       VarInt           "Rotation"
                    read "ignoreEntities" Bool            "IgnoreEntities"
                    read "status"         VarInt           "Status"
                    read "errorMessage"   (Option AnonNbt) "ErrorMessage"
                ]
        }
