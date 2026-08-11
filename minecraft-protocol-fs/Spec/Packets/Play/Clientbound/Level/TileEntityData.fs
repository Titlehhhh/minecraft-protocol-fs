namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module TileEntityData =

    let tileEntityData =
        packet "TileEntityDataPacket" Play Clientbound All {
            api [
                field "Location" (TNamed "Position") All
                field "Action"   TInt                All
                field "NbtData"  (TOption TNbt)      All
            ]

            wire (Until 756) [
                read "location" (Named "Position") "Location"
                read "action"   U8                 "Action"
                read "nbtData"  (Option Nbt)       "NbtData"
            ]

            wire (Between(757, 763)) [
                read "location" (Named "Position") "Location"
                read "action"   VarInt             "Action"
                read "nbtData"  (Option Nbt)       "NbtData"
            ]

            wire (Since 764) [
                read "location" (Named "Position") "Location"
                read "action"   VarInt             "Action"
                read "nbtData"  (Option AnonNbt)   "NbtData"
            ]
        }
