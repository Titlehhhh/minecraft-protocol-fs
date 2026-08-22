namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ItemSoundEvent =

    let itemSoundEvent =
        record "ItemSoundEvent" (Since 761) [ col "soundName" Str; col "fixedRange" (Option F32) ]
