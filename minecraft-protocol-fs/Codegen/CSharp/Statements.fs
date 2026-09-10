namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Statements =

    open type SyntaxFactory
    open Text

    // ----- wire entries -> statements -----

    /// C# type of a wire item when held in a local or array (primitives + named types only).
    let private itemCsType (s: RuntimeSurface) (w: WireType) : string option =
        match w with
        | Named n -> Some n
        | EnumRef(n, _) -> Some n
        | RegistryHolder inner -> holderCsType s inner
        | FixedBytes _ -> Some "byte[]"
        | Array(U8, VarIntCount) -> s.Primitives.TryFind ByteArray |> Option.map (fun p -> p.CsType)
        | _ -> s.Primitives.TryFind w |> Option.map (fun p -> p.CsType)

    /// C# type of a whole wire field, wrappers included — what a union case declares as a
    /// positional parameter. `None` means the shape has no renderer yet.
    let rec internal wireCsType (s: RuntimeSurface) (w: WireType) : string option =
        match w with
        | Option inner -> wireCsType s inner |> Option.map (fun t -> t + "?")
        | Array(item, _)
        | SentinelArray(item, _) -> wireCsType s item |> Option.map (fun t -> t + "[]")
        | _ -> itemCsType s w

    /// Count-prefix read: setup lines + the count expression.
    let private countRead (s: RuntimeSurface) (cnt: ArrayCount) (ln: string) : (string list * string) option =
        match cnt with
        | VarIntCount -> Some([ sprintf "int %sCount = %s.ReadVarInt();" ln s.ReaderParam ], sprintf "%sCount" ln)
        | FixedCount n -> Some([], string n)
        | TypedCount w ->
            s.Primitives.TryFind w
            |> Option.map (fun p ->
                [ sprintf "int %sCount = checked((int)%s.%s);" ln s.ReaderParam p.ReadCall ], sprintf "%sCount" ln)

    /// Count-prefix write lines for a value's `.Length`.
    let private countWrite (s: RuntimeSurface) (cnt: ArrayCount) (value: string) : string list option =
        match cnt with
        | VarIntCount -> Some [ sprintf "%s.WriteVarInt(%s.Length);" s.WriterParam value ]
        | FixedCount _ -> Some []
        | TypedCount w ->
            s.Primitives.TryFind w
            |> Option.map (fun p -> [ sprintf "%s.%s((%s)%s.Length);" s.WriterParam p.WriteMethod p.CsType value ])

    /// `new T[count]` — the count belongs in the *first* pair of brackets, so an item type that is
    /// itself an array (`byte[]`) becomes `new byte[count][]`, never `new byte[][count]`.
    let private newArrayExpr (itemType: string) (count: string) =
        match itemType.IndexOf '[' with
        | -1 -> sprintf "new %s[%s]" itemType count
        | i -> sprintf "new %s[%s]%s" (itemType.Substring(0, i)) count (itemType.Substring i)

    /// Capacity a sentinel-terminated array starts at. No count travels ahead of its items, so
    /// the read has nothing to size the buffer from: start small, double, trim.
    let private sentinelSeed = 4

    /// `T` -> `T?`, idempotent: a conditional group declares its locals nullable so the guard can
    /// leave them unset, and the api field it feeds is optional for exactly the same reason.
    let private nullableOf (t: string) = if t.EndsWith "?" then t else t + "?"

    /// The api names a read entry binds, paired with the C# type of the local it declares. A
    /// conditional group hoists these above its guard; a block passes them to the ctor.
    let rec private entryBindings (s: RuntimeSurface) (entry: WireEntry) : (string * string) list =
        let one api w =
            wireCsType s w |> Option.map (fun t -> [ api, t ]) |> Option.defaultValue []

        match entry with
        | Read(_, w, api) -> one api w
        | ReadOpt(_, w, api, _, _) -> one api w |> List.map (fun (a, t) -> a, nullableOf t)
        | ReadBlock(w, api, _) -> one api w
        | IfNonZero(_, inner) ->
            inner
            |> List.collect (entryBindings s)
            |> List.map (fun (a, t) -> a, nullableOf t)
        // an arm binds what it reads, and only the arm the stream took actually runs — so every
        // arm's fields are hoisted together and every one of them is nullable, exactly like a
        // conditional group's
        | InlineUnion(_, arms) ->
            arms
            |> List.collect (fun arm -> arm.Entries |> List.collect (entryBindings s))
            |> List.distinctBy fst
            |> List.map (fun (a, t) -> a, nullableOf t)
        | Discard _
        | ReadUnion _ -> []

    /// Wire *and* api spellings of every field a layout reads, both pointing at the api name and
    /// the wire type it travelled as. `ifNonZero`/`readOpt` name their discriminator with either
    /// spelling (`ifNonZero "columns"` against `read "columns" U8 "Columns"`), so both must
    /// resolve; the wire type is what lets the write side guard on the *narrowed* value the read
    /// side will see.
    let private fieldNames (entries: WireEntry list) : Map<string, string * WireType> =
        entries
        |> List.collect (function
            | Read(wire, wt, api) -> [ wire, (api, wt); api, (api, wt) ]
            | _ -> [])
        |> Map.ofList

    /// An entry a conditional group cannot hoist: it would read inside the guard, bind nothing,
    /// and leave the api field `default!` while the bytes were consumed. A stub is the honest
    /// answer — `Discard` is the one entry that legitimately binds nothing.
    let private unbindableInGroup (s: RuntimeSurface) (entries: WireEntry list) =
        entries
        |> List.tryFind (function
            | Discard _ -> false
            | e -> entryBindings s e |> List.isEmpty)

    /// The natural api types of a block's own entries — a block's fields belong to the nested
    /// type, not to the packet, so the packet's api map cannot answer narrowing casts for them.
    let private naturalApiTypes (entries: WireEntry list) : Map<string, ApiType> =
        entries
        |> List.choose (function
            | Read(_, w, api) ->
                try
                    Some(api, apiOf w)
                with _ ->
                    None
            | _ -> None)
        |> Map.ofList

    /// How a discriminator local compares against a case key, spelled for the wire it arrived on.
    /// A `Bool` discriminator lands in a C# `bool`, where `local == 1` would not even compile.
    let private discEquals (wt: WireType option) (local: string) (key: int) =
        match wt with
        | Some Bool -> if key = 0 then sprintf "!%s" local else local
        | _ -> sprintf "%s == %d" local key

    /// `field != 0` the way a conditional group spells it — or the bare local when the field
    /// travelled as a `Bool`, which C# refuses to compare against an integer at all.
    let private nonZeroCond (wt: WireType option) (local: string) =
        match wt with
        | Some Bool -> local
        | _ -> sprintf "%s != 0" local

    /// Guards for the arms of an inline union, in ladder order. `None` is the bare `else`, which
    /// the last arm takes when the arms already cover every value the discriminator can carry (a
    /// `Bool` read under both keys) — that is what keeps `if (flag) ... else ...` from growing a
    /// branch no stream can reach. Otherwise every arm is guarded and the caller adds the throw.
    let private armConditions
        (discLocal: string)
        (discWt: WireType option)
        (arms: UnionArm list)
        : string option list
        =
        // a `Bool` splits into exactly two values, so two arms pinned to one value each are the
        // only shape that leaves nothing over; anything looser and the `else` would swallow a key
        // some other arm claims
        let truths (arm: UnionArm) = arm.Keys |> List.map (fun k -> k <> 0) |> List.distinct

        let covered =
            discWt = Some Bool
            && List.length arms = 2
            && arms |> List.forall (fun arm -> List.length (truths arm) = 1)
            && List.length (arms |> List.collect truths |> List.distinct) = 2

        let last = List.length arms - 1

        arms
        |> List.mapi (fun i arm ->
            if covered && i = last then
                None
            else
                arm.Keys
                |> List.map (discEquals discWt discLocal)
                |> String.concat " || "
                |> Some)

    /// One wire entry -> read statement lines. `bound` maps every name the entries before this one
    /// in the same layout already read (wire and api spelling) to its api name and the wire it
    /// travelled as — the only names an entry may address. The wire type is `None` for a name a
    /// non-`Read` entry bound: nothing addresses those as a discriminator, and a shape that needs
    /// the wire to spell a comparison says so instead of guessing. `nameOf` turns an api name into
    /// the local holding it, which is how a group renders its body into fresh locals before
    /// assigning the hoisted ones.
    let rec private readEntryLines
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (bound: Map<string, string * WireType option>)
        (entry: WireEntry)
        : Result<string list, string>
        =
        match entry with
        | Read(_, Option inner, api) ->
            let ln = nameOf api

            match readExpr s inner, itemCsType s inner with
            | Ok call, Some t ->
                Ok
                    [
                        sprintf "%s? %s = null;" t ln
                        sprintf "if (%s.ReadBoolean()) %s = %s;" s.ReaderParam ln call
                    ]
            | _ -> Error(sprintf "read '%s' (Option %A)" api inner)
        | Read(_, Array(item, cnt), api) ->
            let ln = nameOf api

            match readExpr s item, itemCsType s item, countRead s cnt ln with
            | Ok call, Some t, Some(setup, cntExpr) ->
                Ok(
                    setup
                    @ [
                        sprintf "var %s = %s;" ln (newArrayExpr t cntExpr)
                        sprintf "for (int i = 0; i < %s.Length; i++) %s[i] = %s;" ln ln call
                    ]
                )
            | _ -> Error(sprintf "read '%s' (Array %A)" api item)
        | Read(_, SentinelArray(item, endValue), api) ->
            // No count travels ahead of the items, so the buffer grows by doubling and is trimmed
            // to what was actually read - `NbtBinaryReader.ReadArrayBigEndian`'s shape, minus the
            // length it knows up front. The terminator occupies the byte where the next item would
            // start, so the loop tests that byte and rewinds it when it belongs to an item.
            let ln = nameOf api

            match readExpr s item, itemCsType s item, s.Primitives.TryFind U8 with
            | Ok call, Some t, Some sentinel ->
                Ok
                    [
                        sprintf "var %s = %s;" ln (newArrayExpr t (string sentinelSeed))
                        sprintf "int %sCount = 0;" ln
                        sprintf "while (%s.%s != %d)" s.ReaderParam sentinel.ReadCall endValue
                        "{"
                        sprintf "%s.%s(1);" s.ReaderParam s.RewindMethod
                        sprintf "if (%sCount == %s.Length) System.Array.Resize(ref %s, %s.Length * 2);" ln ln ln ln
                        sprintf "%s[%sCount++] = %s;" ln ln call
                        "}"
                        sprintf "if (%s.Length != %sCount) System.Array.Resize(ref %s, %sCount);" ln ln ln ln
                    ]
            | _ -> Error(sprintf "read '%s' (SentinelArray %A)" api item)
        | Read(_, wt, api) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "var %s = %s;" (nameOf api) call ]
            | Error e -> Error(sprintf "read '%s' (%s)" api e)
        | Discard(wire, Option inner) ->
            // wrappers nest, so the inner shape is discarded by the same renderer one level down
            match readEntryLines s nameOf bound (Discard(wire, inner)) with
            | Ok ls -> Ok([ sprintf "if (%s.ReadBoolean())" s.ReaderParam; "{" ] @ ls @ [ "}" ])
            | Error e -> Error(sprintf "discard '%s' (Option: %s)" wire e)
        | Discard(wire, Array(item, cnt)) ->
            let ln = "skip" + pascal (camel wire)

            match countRead s cnt ln, readEntryLines s nameOf bound (Discard(wire + "Item", item)) with
            | Some(setup, cntExpr), Ok ls ->
                Ok(
                    setup
                    @ [ sprintf "for (int %sI = 0; %sI < %s; %sI++)" ln ln cntExpr ln; "{" ]
                    @ ls
                    @ [ "}" ]
                )
            | _, Error e -> Error(sprintf "discard '%s' (Array: %s)" wire e)
            | None, _ -> Error(sprintf "discard '%s' (Array count %A)" wire cnt)
        | Discard(wire, wt) ->
            match readExpr s wt with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | ReadUnion(disc, _, api) when (bound.TryFind disc).IsNone ->
            // Nothing read '%s' yet, so the emitted call would name a local that does not exist:
            // valid-looking C# that cannot compile. A stub is the honest answer.
            Error(sprintf "read union '%s' (discriminator '%s' is not read by an earlier entry)" api disc)
        | ReadUnion(disc, unionName, api) ->
            // the discriminator is a plain wire field an earlier entry already read into a local;
            // the cast normalizes whatever integer it was to the union's `int discriminator`
            Ok
                [
                    sprintf
                        "var %s = %s.%s(ref %s, %s, (int)%s);"
                        (nameOf api)
                        unionName
                        s.ReadMethodName
                        s.ReaderParam
                        s.VersionParam
                        (nameOf (fst bound.[disc]))
                ]
        | IfNonZero(field, entries) ->
            match bound.TryFind field with
            | None -> Error(sprintf "conditional group (field '%s' is not read by an earlier entry)" field)
            | Some(api, wt) -> readGroupLines s nameOf bound (nonZeroCond wt (nameOf api)) entries
        | ReadOpt(wire, wt, api, disc, keys) ->
            match bound.TryFind disc with
            | None -> Error(sprintf "read optional '%s' (discriminator '%s' is not read by an earlier entry)" api disc)
            | Some(discApi, _) ->
                let cond =
                    keys
                    |> List.map (fun k -> sprintf "%s == %d" (nameOf discApi) k)
                    |> String.concat " || "

                readGroupLines s nameOf bound cond [ Read(wire, wt, api) ]
        | ReadBlock(Named typeName, api, entries) ->
            let ln = nameOf api
            let innerName (a: string) = ln + pascal a
            let results = readEntriesFold s innerName bound entries

            match firstReadError results, unbindableInGroup s entries with
            | Some e, _ -> Error(sprintf "read block '%s' (%s)" api e)
            | _, Some e -> Error(sprintf "read block '%s' holds an entry it cannot pass to the ctor: %A" api e)
            | None, None ->
                // positional: the ctor parameter *names* differ by shell (positional record struct
                // keeps the api name, class camel-cases it), so a block's entries must be listed
                // in the target type's api order — which is how a container is modelled anyway.
                let args =
                    entries
                    |> List.collect (entryBindings s)
                    |> List.map (fun (a, _) -> innerName a)
                    |> String.concat ", "

                Ok(readLinesOf results @ [ sprintf "var %s = new %s(%s);" ln typeName args ])
        | InlineUnion(disc, arms) ->
            match bound.TryFind disc with
            | None -> Error(sprintf "inline union (discriminator '%s' is not read by an earlier entry)" disc)
            | Some(discApi, discWt) -> readInlineUnionLines s nameOf bound disc (nameOf discApi) discWt arms
        | other -> Error(sprintf "%A" other)

    /// A conditional group: every local the body binds is declared (nullable) above the guard, the
    /// body reads into its own locals inside it, and the hoisted ones are assigned at the end. The
    /// inner names keep the C# legal — a nested scope may not shadow an outer local.
    and private readGroupLines
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (bound: Map<string, string * WireType option>)
        (cond: string)
        (entries: WireEntry list)
        : Result<string list, string>
        =
        let innerName (a: string) = nameOf a + "Value"
        let results = readEntriesFold s innerName bound entries

        match firstReadError results, unbindableInGroup s entries with
        | Some e, _ -> Error e
        | _, Some e -> Error(sprintf "conditional group holds an entry it cannot hoist: %A" e)
        | None, None ->
            let bindings = entries |> List.collect (entryBindings s)

            Ok(
                [ for a, t in bindings -> sprintf "%s %s = default;" (nullableOf t) (nameOf a) ]
                @ [ sprintf "if (%s)" cond; "{" ]
                @ readLinesOf results
                @ [ for a, _ in bindings -> sprintf "%s = %s;" (nameOf a) (innerName a) ]
                @ [ "}" ]
            )

    /// An inline union is not a C# type - it is a ladder of conditional groups over one
    /// discriminator, which is why it renders here beside `readGroupLines` and not in the union
    /// backend. Every api field any arm binds is hoisted (nullable) above the ladder, so the arm
    /// the stream selected assigns its own and leaves the rest null: exactly what the `TOption`
    /// api fields such a layout declares already say. Each arm reads inside its own block, so two
    /// arms may reuse a local name.
    and private readInlineUnionLines
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (bound: Map<string, string * WireType option>)
        (disc: string)
        (discLocal: string)
        (discWt: WireType option)
        (arms: UnionArm list)
        : Result<string list, string>
        =
        let innerName (a: string) = nameOf a + "Value"

        let armBindings (arm: UnionArm) =
            arm.Entries |> List.collect (entryBindings s) |> List.distinctBy fst

        let rendered = arms |> List.map (fun arm -> arm, readEntriesFold s innerName bound arm.Entries)

        let armError =
            rendered
            |> List.tryPick (fun (arm, rs) ->
                firstReadError rs |> Option.map (sprintf "inline union arm '%s' (%s)" arm.Name))

        let unhoistable =
            arms
            |> List.tryPick (fun arm ->
                unbindableInGroup s arm.Entries
                |> Option.map (fun e -> sprintf "inline union arm '%s' holds an entry it cannot hoist: %A" arm.Name e))

        match armError, unhoistable, arms |> List.tryFind (fun arm -> List.isEmpty arm.Keys) with
        | Some e, _, _
        | _, Some e, _ -> Error e
        | _, _, Some arm -> Error(sprintf "inline union arm '%s' has no discriminator key" arm.Name)
        | None, None, None ->
            let conds = armConditions discLocal discWt arms

            let ladder =
                [
                    for i, ((arm, rs), cond) in List.indexed (List.zip rendered conds) do
                        match cond with
                        | Some c when i = 0 -> yield sprintf "if (%s)" c
                        | Some c -> yield sprintf "else if (%s)" c
                        | None -> yield "else"

                        yield "{"
                        yield! readLinesOf rs
                        yield! [ for a, _ in armBindings arm -> sprintf "%s = %s;" (nameOf a) (innerName a) ]
                        yield "}"
                ]

            let fallthrough =
                if conds |> List.exists Option.isNone then
                    []
                else
                    [ "else"; "{"; throwNoInlineCaseLine s disc discLocal; "}" ]

            Ok(
                [
                    for a, t in arms |> List.collect armBindings |> List.distinctBy fst ->
                        sprintf "%s %s = default;" (nullableOf t) (nameOf a)
                ]
                @ ladder
                @ fallthrough
            )

    /// Read entries in layout order, pairing each with its rendered lines. The fold is what makes
    /// "an earlier entry bound this" checkable: an entry only ever sees the names before it.
    and private readEntriesFold
        (s: RuntimeSurface)
        (nameOf: string -> string)
        (start: Map<string, string * WireType option>)
        (entries: WireEntry list)
        : (WireEntry * Result<string list, string>) list
        =
        entries
        |> List.mapFold
            (fun bound e ->
                let rendered = readEntryLines s nameOf bound e

                let bound =
                    match e with
                    | Read(wire, wt, api) -> bound |> Map.add wire (api, Some wt) |> Map.add api (api, Some wt)
                    | _ -> entryBindings s e |> List.fold (fun m (a, _) -> Map.add a (a, None) m) bound

                (e, rendered), bound)
            start
        |> fst

    and private firstReadError (results: (WireEntry * Result<string list, string>) list) : string option =
        results
        |> List.tryPick (function
            | _, Error e -> Some e
            | _ -> None)

    and internal readLinesOf (results: (WireEntry * Result<string list, string>) list) : string list =
        results
        |> List.collect (function
            | _, Ok ls -> ls
            | _, Error _ -> [])

    let internal readEntriesLines (s: RuntimeSurface) (entries: WireEntry list) =
        readEntriesFold s (localName s) Map.empty entries

    /// Locals a rendered layout leaves behind, by local name — the set a ctor call may draw on.
    let internal boundLocals (s: RuntimeSurface) (results: (WireEntry * Result<string list, string>) list) =
        results
        |> List.collect (function
            | ReadUnion(_, _, api), Ok _ -> [ localName s api ]
            | e, Ok _ -> entryBindings s e |> List.map (fst >> localName s)
            | _, Error _ -> [])
        |> Set.ofList

    let internal hasReadError (results: (WireEntry * Result<string list, string>) list) =
        results
        |> List.exists (function
            | _, Error _ -> true
            | _ -> false)

    /// Wire-only discriminator -> the api field of the union that consumes it. The write side has
    /// no such wire field in the model, so the union's own `Discriminator(pv)` is its only source.
    let private discriminatorUnions (entries: WireEntry list) : Map<string, string> =
        entries
        |> List.choose (function
            | ReadUnion(disc, _, api) -> Some(disc, api)
            | _ -> None)
        |> Map.ofList

    /// Discriminator -> the arms of the inline union that consumes it. The same job
    /// `discriminatorUnions` does, for the union that has no C# type of its own: no member of the
    /// model answers `Discriminator(pv)`, so the key has to be derived from the fields that are set.
    /// Only a union whose discriminator an *earlier* entry read is in here: the derived key
    /// lands in a local, and a local cannot be read above the line that declares it. A layout
    /// that orders them the other way stubs — the read side rejects it for the same reason.
    let private inlineUnionArms (entries: WireEntry list) : Map<string, UnionArm list> =
        entries
        |> List.mapFold
            (fun seen e ->
                let taken =
                    match e with
                    | InlineUnion(disc, arms) when Set.contains disc seen -> Some(disc, arms)
                    | _ -> None

                let seen =
                    match e with
                    | Read(wire, _, api) -> seen |> Set.add wire |> Set.add api
                    | _ -> seen

                taken, seen)
            Set.empty
        |> fst
        |> List.choose id
        |> Map.ofList

    /// What a write body needs beyond the entry itself. `Access` is how a value is spelled in C#:
    /// the api name at the top level, `block.Field` inside a block — the one place a nested
    /// container differs from the packet's own fields.
    type internal WriteCtx =
        {
            ApiTypes: Map<string, ApiType>
            DiscUnions: Map<string, string>
            InlineArms: Map<string, UnionArm list>
            Fields: Map<string, string * WireType>
            Access: string -> string
        }

    let internal writeCtxOf (apiTypes: Map<string, ApiType>) (entries: WireEntry list) : WriteCtx =
        {
            ApiTypes = apiTypes
            DiscUnions = discriminatorUnions entries
            InlineArms = inlineUnionArms entries
            Fields = fieldNames entries
            Access = id
        }

    /// The value a conditional guard tests, spelled exactly as the wire will carry it. The read
    /// side guards on the byte it read back, so a wider api value must be narrowed here too:
    /// `Flag = 256` over a `U8` wire writes `0`, and an un-narrowed `Flag != 0` would then write a
    /// block the reader never looks for — a silent stream desync.
    let private discValue (s: RuntimeSurface) (ctx: WriteCtx) (api: string) (wt: WireType) : string =
        let acc = ctx.Access api

        let apiT =
            match ctx.ApiTypes.TryFind api with
            | Some(TOption t) -> Some t
            | other -> other

        match s.Primitives.TryFind wt, apiT with
        | Some p, Some t when csType s t <> p.CsType -> sprintf "(%s)%s" p.CsType acc
        | _ -> acc

    /// The test that says the model carries this arm: every optional api field the arm reads is
    /// set. An arm with no optional field of its own cannot answer that question, and a derivation
    /// that guessed would put a key on the wire the payload behind it does not match - so the arm,
    /// and with it the whole inline union, stays a gap instead.
    let private armPresence (s: RuntimeSurface) (ctx: WriteCtx) (arm: UnionArm) : Result<string, string> =
        let optionals =
            arm.Entries
            |> List.collect (entryBindings s)
            |> List.map fst
            |> List.distinct
            |> List.filter (fun n ->
                match ctx.ApiTypes.TryFind n with
                | Some(TOption _) -> true
                | _ -> false)

        if List.isEmpty optionals then
            Error(sprintf "inline union arm '%s' has no optional api field its key could be derived from" arm.Name)
        else
            optionals
            |> List.map (fun n -> sprintf "%s is not null" (ctx.Access n))
            |> String.concat " && "
            |> Ok

    /// The wire-only key of an inline union, derived from the model: the first arm whose api
    /// fields are all present wins, and a model that matches no arm fails loudly rather than
    /// writing a key the bytes behind it will contradict. The key lands in a local typed as the
    /// wire carries it, so the union body right after it guards on the very value that went out.
    let private inlineDiscLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (disc: string)
        (wt: WireType)
        (arms: UnionArm list)
        : Result<string list, string>
        =
        let ln = localName s disc
        let presence = arms |> List.map (fun arm -> arm, armPresence s ctx arm)

        let firstError =
            presence
            |> List.tryPick (fun (_, r) ->
                match r with
                | Error e -> Some e
                | Ok _ -> None)

        match firstError, s.Primitives.TryFind wt, arms |> List.tryFind (fun a -> List.isEmpty a.Keys) with
        | Some e, _, _ -> Error e
        | _, None, _ -> Error(sprintf "write inline union key '%s' (%A is not a wire primitive)" disc wt)
        | _, _, Some arm -> Error(sprintf "inline union arm '%s' has no discriminator key" arm.Name)
        | None, Some p, None ->
            let lit (k: int) =
                if p.CsType = "bool" then (if k = 0 then "false" else "true")
                elif p.CsType = csType s TInt then string k
                else sprintf "(%s)%d" p.CsType k

            let expr =
                List.foldBack
                    (fun ((arm: UnionArm), r) acc ->
                        match r with
                        | Ok cond -> sprintf "%s ? %s : %s" cond (lit (List.head arm.Keys)) acc
                        | Error _ -> acc)
                    presence
                    (sprintf
                        "throw new System.InvalidOperationException(\"No inline union case selected by '%s' matches the fields that are set.\")"
                        disc)

            match writeExpr s wt None ln with
            | Ok call -> Ok [ sprintf "%s %s = %s;" p.CsType ln expr; sprintf "%s;" call ]
            | Error e -> Error(sprintf "write inline union key '%s' (%s)" disc e)

    /// One wire entry -> write statement lines. `ctx.ApiTypes` drives narrowing casts;
    /// `ctx.DiscUnions` pairs a wire-only discriminator with the union field that derives its value.
    let rec internal writeEntryLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (entry: WireEntry)
        : Result<string list, string>
        =
        let apiTypes = ctx.ApiTypes
        let discUnions = ctx.DiscUnions

        match entry with
        | Read(_, wt, api) when api.StartsWith "_" ->
            // wire-only field: the model carries no such field, so the value must be derived. Only
            // a union consumer knows how — anything else stays a gap.
            match discUnions.TryFind api, ctx.InlineArms.TryFind api with
            | None, Some arms -> inlineDiscLines s ctx api wt arms
            | None, None -> Error(sprintf "write wire-only '%s' (derive from model)" api)
            | Some unionApi, _ ->
                let disc = sprintf "%s.%s(%s)" unionApi s.DiscriminatorMethodName s.VersionParam

                // `Discriminator` hands back the api-level integer; a narrower wire primitive must
                // report a key it cannot carry instead of wrapping it (same rule as `countRead`).
                let value =
                    match s.Primitives.TryFind wt with
                    | Some p when p.CsType <> csType s TInt -> sprintf "checked((%s)%s)" p.CsType disc
                    | _ -> disc

                match writeExpr s wt None value with
                | Ok call -> Ok [ sprintf "%s;" call ]
                | Error e -> Error(sprintf "write discriminator '%s' (%s)" api e)
        | Read(_, Option inner, api) ->
            let v = camel api + "Value"
            let acc = ctx.Access api

            match writeExpr s inner None v with
            | Ok call ->
                Ok
                    [
                        sprintf "%s.WriteBoolean(%s is not null);" s.WriterParam acc
                        sprintf "if (%s is { } %s) %s;" acc v call
                    ]
            | Error e -> Error(sprintf "write '%s' (Option: %s)" api e)
        | Read(_, Array(item, cnt), api) ->
            let iv = camel api + "Item"

            // same rule as the scalar branch below: an option-typed api field written as a
            // required wire value must be present, or the count and the items disagree. The
            // count and the loop both read it, so bind it once instead of repeating the throw.
            let bind, acc =
                match apiTypes.TryFind api with
                | Some(TOption _) ->
                    let lv = camel api + "Value"

                    [
                        sprintf
                            "var %s = %s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\");"
                            lv
                            (ctx.Access api)
                            api
                    ],
                    lv
                | _ -> [], ctx.Access api

            match writeExpr s item None iv, countWrite s cnt acc with
            | Ok call, Some cw -> Ok(bind @ cw @ [ sprintf "foreach (var %s in %s) %s;" iv acc call ])
            | _ -> Error(sprintf "write '%s' (Array %A)" api item)
        | Read(_, SentinelArray(item, endValue), api) ->
            // the items, then the terminator; no count to write ahead of them, which is the whole
            // difference from `Array` - and the reason a missing value has to throw here too
            let iv = camel api + "Item"

            let bind, acc =
                match apiTypes.TryFind api with
                | Some(TOption _) ->
                    let lv = camel api + "Value"

                    [
                        sprintf
                            "var %s = %s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\");"
                            lv
                            (ctx.Access api)
                            api
                    ],
                    lv
                | _ -> [], ctx.Access api

            match writeExpr s item None iv, s.Primitives.TryFind U8 with
            | Ok call, Some sentinel ->
                match writeExpr s U8 (Some sentinel.CsType) (string endValue) with
                | Ok endCall ->
                    Ok(bind @ [ sprintf "foreach (var %s in %s) %s;" iv acc call; sprintf "%s;" endCall ])
                | Error e -> Error(sprintf "write '%s' (SentinelArray terminator: %s)" api e)
            | _ -> Error(sprintf "write '%s' (SentinelArray %A)" api item)
        | Read(_, wt, api) ->
            // an option-typed api field written as a required wire value must be present
            let apiT = apiTypes.TryFind api

            let requiredT =
                match apiT with
                | Some(TOption t) -> Some t
                | other -> other

            let acc = ctx.Access api

            let value =
                match apiT with
                | Some(TOption _) ->
                    sprintf
                        "(%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\"))"
                        acc
                        api
                | _ -> acc
            // narrow explicitly when the api type is wider than the wire primitive (int api, i8 wire)
            let cast =
                match s.Primitives.TryFind wt, requiredT with
                | Some p, Some t when csType s t <> p.CsType -> Some p.CsType
                | _ -> None

            match writeExpr s wt cast value with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "write '%s' (%s)" api e)
        | Discard(_, Option _) -> Ok [ sprintf "%s.WriteBoolean(false);" s.WriterParam ]
        | Discard(wire, Array(_, cnt)) ->
            match cnt with
            | VarIntCount -> Ok [ sprintf "%s.WriteVarInt(0);" s.WriterParam ]
            | TypedCount w ->
                match s.Primitives.TryFind w with
                | Some p -> Ok [ sprintf "%s.%s(0);" s.WriterParam p.WriteMethod ]
                | None -> Error(sprintf "discard '%s' (Array count %A)" wire w)
            | FixedCount _ -> Error(sprintf "discard '%s' (fixed-count array needs real items)" wire)
        | Discard(wire, wt) ->
            let value =
                match wt with
                | Str -> "\"\""
                | FixedBytes n -> sprintf "new byte[%d]" n
                | _ -> "default"

            match writeExpr s wt None value with
            | Ok call -> Ok [ sprintf "%s;" call ]
            | Error e -> Error(sprintf "discard '%s' (%s)" wire e)
        | ReadUnion(_, _, api) ->
            Ok
                [
                    sprintf "%s.%s(%s, %s);" (ctx.Access api) s.WriteMethodName s.WriterParam s.VersionParam
                ]
        | IfNonZero(field, entries) ->
            match ctx.Fields.TryFind field with
            | None -> Error(sprintf "write conditional group (field '%s' is not a wire field of this layout)" field)
            | Some(api, wt) -> writeGroupLines s ctx (nonZeroCond (Some wt) (discValue s ctx api wt)) entries
        | ReadOpt(wire, wt, api, disc, keys) ->
            match ctx.Fields.TryFind disc with
            | None ->
                Error(sprintf "write optional '%s' (discriminator '%s' is not a wire field of this layout)" api disc)
            | Some(discApi, discWt) ->
                let v = discValue s ctx discApi discWt

                let cond = keys |> List.map (sprintf "%s == %d" v) |> String.concat " || "

                match writeGroupLines s ctx cond [ Read(wire, wt, api) ] with
                | Error e -> Error e
                | Ok lines ->
                    // the mirror of the `?? throw` inside the guard: a value the discriminator
                    // says is absent has nowhere to go, and dropping it silently loses data
                    let elseThrow =
                        match ctx.ApiTypes.TryFind api with
                        | Some(TOption _) ->
                            [
                                sprintf "else if (%s is not null)" (ctx.Access api)
                                "{"
                                sprintf
                                    "throw new System.InvalidOperationException(\"%s is set, but '%s' does not select it at this protocol version.\");"
                                    api
                                    disc
                                "}"
                            ]
                        | _ -> []

                    Ok(lines @ elseThrow)
        | ReadBlock(Named typeName, api, entries) ->
            let ln = localName s api

            let inner =
                {
                    ApiTypes = naturalApiTypes entries
                    DiscUnions = Map.empty
                    InlineArms = inlineUnionArms entries
                    Fields = fieldNames entries
                    Access = fun a -> ln + "." + a
                }

            let results = entries |> List.map (writeEntryLines s inner)

            match
                results
                |> List.tryPick (function
                    | Error e -> Some e
                    | _ -> None)
            with
            | Some e -> Error(sprintf "write block '%s' (%s)" api e)
            | None ->
                let source =
                    match ctx.ApiTypes.TryFind api with
                    | Some(TOption _) ->
                        sprintf
                            "%s ?? throw new System.InvalidOperationException(\"%s is required at this protocol version.\")"
                            (ctx.Access api)
                            api
                    | _ -> ctx.Access api

                Ok(
                    [ sprintf "%s %s = %s;" typeName ln source ]
                    @ (results
                       |> List.collect (function
                           | Ok ls -> ls
                           | Error _ -> []))
                )
        | InlineUnion(disc, arms) ->
            match ctx.Fields.TryFind disc with
            | None -> Error(sprintf "write inline union (discriminator '%s' is not a wire field of this layout)" disc)
            | Some(discApi, _) when discApi.StartsWith "_" && not (ctx.InlineArms.ContainsKey disc) ->
                Error(sprintf "write inline union (discriminator '%s' is not derived by an earlier entry)" disc)
            | Some(discApi, discWt) ->
                // a wire-only key was derived into a local of the wire's own C# type by the entry
                // just above; a key the api models comes off the model, narrowed the way the wire
                // carries it. Either way the guard tests exactly the value that went out.
                let value =
                    if discApi.StartsWith "_" then
                        localName s discApi
                    else
                        discValue s ctx discApi discWt

                writeInlineUnionLines s ctx disc value discWt arms
        | other -> Error(sprintf "%A" other)

    /// An inline union on the write side: the same ladder the read side builds, over the same
    /// discriminator value. Each arm writes its own fields, which the api models as optional, so
    /// the `?? throw` the scalar renderer already emits is what catches a model whose fields and
    /// whose key disagree.
    and private writeInlineUnionLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (disc: string)
        (discAccess: string)
        (discWt: WireType)
        (arms: UnionArm list)
        : Result<string list, string>
        =
        let rendered =
            arms |> List.map (fun arm -> arm, arm.Entries |> List.map (writeEntryLines s ctx))

        let armError =
            rendered
            |> List.tryPick (fun (arm, rs) ->
                rs
                |> List.tryPick (function
                    | Error e -> Some(sprintf "inline union arm '%s' (%s)" arm.Name e)
                    | Ok _ -> None))

        match armError, arms |> List.tryFind (fun arm -> List.isEmpty arm.Keys) with
        | Some e, _ -> Error e
        | _, Some arm -> Error(sprintf "inline union arm '%s' has no discriminator key" arm.Name)
        | None, None ->
            let conds = armConditions discAccess (Some discWt) arms

            let ladder =
                [
                    for i, ((_, rs), cond) in List.indexed (List.zip rendered conds) do
                        match cond with
                        | Some c when i = 0 -> yield sprintf "if (%s)" c
                        | Some c -> yield sprintf "else if (%s)" c
                        | None -> yield "else"

                        yield "{"

                        yield!
                            rs
                            |> List.collect (function
                                | Ok ls -> ls
                                | Error _ -> [])

                        yield "}"
                ]

            if conds |> List.exists Option.isNone then
                Ok ladder
            else
                Ok(ladder @ [ "else"; "{"; throwNoInlineCaseLine s disc discAccess; "}" ])

    /// A conditional group on the write side: the same guard the read side applies, over the api
    /// value the group's discriminator field carries.
    and private writeGroupLines
        (s: RuntimeSurface)
        (ctx: WriteCtx)
        (cond: string)
        (entries: WireEntry list)
        : Result<string list, string>
        =
        let results = entries |> List.map (writeEntryLines s ctx)

        match
            results
            |> List.tryPick (function
                | Error e -> Some e
                | _ -> None)
        with
        | Some e -> Error e
        | None ->
            Ok(
                [ sprintf "if (%s)" cond; "{" ]
                @ (results
                   |> List.collect (function
                       | Ok ls -> ls
                       | Error _ -> []))
                @ [ "}" ]
            )
