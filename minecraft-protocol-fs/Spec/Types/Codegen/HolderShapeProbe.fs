namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module HolderShapeProbe =

    let holderShapeProbe =
        record
            "HolderShapeProbe"
            (Since 761)
            [
                col "before" VarInt
                col "sound" (RegistryHolder(Named "ItemSoundEvent"))
                col "sounds" (Array(RegistryHolder(Named "ItemSoundEvent"), VarIntCount))
                col "after" VarInt
            ]
