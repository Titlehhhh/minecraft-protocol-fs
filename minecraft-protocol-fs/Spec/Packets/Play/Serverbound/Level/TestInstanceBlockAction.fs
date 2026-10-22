namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TestInstanceBlockAction =

    let testInstanceBlockAction =
        packet "TestInstanceBlockActionPacket" Play Serverbound (Since 770) {
            api
                [
                    field "Position" (TNamed "Position") All
                    field "Action"   TInt               All
                    field "Data"     (TNamed "TestInstanceBlockActionData") All
                ]

            wire
                (Since 770)
                [
                    read "pos"    (Named "Position") "Position"
                    read "action" VarInt             "Action"

                    readBlock (Named "TestInstanceBlockActionData") "Data" [
                        read "test"           (Option Str)     "Test"
                        read "size"           (Named "Vec3i")   "Size"
                        read "rotation"       VarInt           "Rotation"
                        read "ignoreEntities" Bool            "IgnoreEntities"
                        read "status"         VarInt           "Status"
                        read "errorMessage"   (Option AnonNbt) "ErrorMessage"
                    ]
                ]
        }
