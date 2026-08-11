namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TestInstanceBlockStatus =

    let testInstanceBlockStatus =
        packet "TestInstanceBlockStatusPacket" Play Clientbound (Since 770) {
            api [
                field "Status" TNbt                      All
                field "Size"   (TOption(TNamed "Vec3i")) All
            ]

            wire (Since 770) [
                read "status" AnonNbt                  "Status"
                read "size"   (Option(Named "Vec3i"))  "Size"
            ]
        }
