namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ProfileProperty =

    let profileProperty =
        record "ProfileProperty" (Since 759) [
            col "name"      Str
            col "value"     Str
            col "signature" (Option Str)
        ]
