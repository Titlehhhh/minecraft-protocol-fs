namespace McProtocol.Dsl

/// Small constructor helpers for api fields and wire entries.
/// Generic sugar over the AST — no concrete protocol content lives here.
[<AutoOpen>]
module Helpers =

    // ===== API =====

    let field name t present : ApiField =
        { Name = name; Type = t; Present = present }


    // ===== WIRE =====

    let read wire t api =
        Read(wire, t, api)

    let discard wire t =
        Discard(wire, t)

    let ifNonZero field entries =
        IfNonZero(field, entries)

    let readOpt wire t api disc keys =
        ReadOpt(wire, t, api, disc, keys)

    let readBlock typ api entries =
        ReadBlock(typ, api, entries)

    let readUnion disc unionName api =
        ReadUnion(disc, unionName, api)

    let inlineUnion disc arms =
        InlineUnion(disc, arms)

    let arm keys name entries =
        { Keys = keys; Name = name; Entries = entries }

    let case keys name entries =
        arm keys name entries

    let case1 key name entries =
        arm [key] name entries
