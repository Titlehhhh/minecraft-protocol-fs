namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module GlobalPos =

    let globalPos =
        record "GlobalPos" (Since 773) [ col "dimensionName" Str; col "location" (Named "Position") ]
