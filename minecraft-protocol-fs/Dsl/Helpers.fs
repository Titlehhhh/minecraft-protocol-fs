namespace McProtocol.Dsl

/// Small constructor helpers for api fields and wire entries.
/// Generic sugar over the AST — no concrete protocol content lives here.
[<AutoOpen>]
module Helpers =

    // ===== API =====

    let field name t present : ApiField =
        {
            Name = name
            Type = t
            Present = present
        }


    // ===== WIRE =====

    let read wire t api = Read(wire, t, api)

    let discard wire t = Discard(wire, t)

    let ifNonZero field entries = IfNonZero(field, entries)

    let readOpt wire t api disc keys = ReadOpt(wire, t, api, disc, keys)

    let readBlock typ api entries = ReadBlock(typ, api, entries)

    let readUnion disc unionName api = ReadUnion(disc, unionName, api)

    let inlineUnion disc arms = InlineUnion(disc, arms)

    let arm keys name entries =
        {
            Keys = keys
            Name = name
            Entries = entries
        }

    let case keys name entries = arm keys name entries

    let case1 key name entries = arm [ key ] name entries


    // ===== RECORD SUGAR =====
    // For plain 1:1 types: one version range, wire order == api order, every wire field maps
    // to exactly one api field. The api is *derived* from the wire, because WireType -> ApiType
    // is total (each wire type has one natural api type) whereas the reverse is ambiguous.

    /// The natural api type of a wire type. Total over the "flat" subset; anything that needs a
    /// deliberate api decision (Switch, SentinelArray, Void, ...) is rejected so it falls back to
    /// an explicit `namedType { api ...; wire ... }`.
    let rec apiOf (w: WireType) : ApiType =
        match w with
        | Bool -> TBool
        | VarInt
        | I8
        | U8
        | I16
        | U16
        | I32
        | U32 -> TInt
        | VarLong
        | I64
        | U64 -> TLong
        | F32 -> TFloat
        | F64 -> TDouble
        | Str -> TString
        | Uuid -> TUuid
        | Nbt
        | AnonNbt -> TNbt
        | ByteArray
        | FixedBytes _
        | RestBytes -> TBytes
        | Option t -> TOption(apiOf t)
        | Array(t, _) -> TArray(apiOf t)
        | Named n -> TNamed n
        | other -> failwithf "apiOf: no natural api type for %A - use explicit api+wire" other

    type Column =
        {
            Wire: string
            Type: WireType
            Api: string
        }

    let private pascal (s: string) =
        if s.Length = 0 then
            s
        else
            string (System.Char.ToUpper s.[0]) + s.[1..]

    /// A wire field whose api name is Pascal(wire) and api type is derived from the wire type.
    let col wire t =
        {
            Wire = wire
            Type = t
            Api = pascal wire
        }

    /// Same as `col`, but with an explicit api name (when Pascal(wire) isn't what you want).
    let colAs wire t api = { Wire = wire; Type = t; Api = api }

    /// A named type whose api is derived from a single, straight-through wire layout.
    let record name range (cols: Column list) : NamedTypeSpec =
        {
            Name = name
            ApiFields =
                cols
                |> List.map (fun c ->
                    {
                        Name = c.Api
                        Type = apiOf c.Type
                        Present = range
                    })
            Layouts =
                [
                    {
                        Range = range
                        Entries = cols |> List.map (fun c -> Read(c.Wire, c.Type, c.Api))
                    }
                ]
        }
