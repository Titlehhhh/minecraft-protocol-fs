namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GameRule =

    let gameRule = record "GameRule" (Since 775) [ col "name" Str; col "value" Str ]
