namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CustomClickActionPlay =

    let customClickActionPlay =
        packet "CustomClickActionPacket" Play Serverbound (Since 771) {
            api [
                field "Id"  TString        All
                field "Nbt" (TOption TNbt) All
            ]

            wire (Since 771) [
                read "id"  Str              "Id"
                read "nbt" (Option AnonNbt) "Nbt"
            ]
        }
