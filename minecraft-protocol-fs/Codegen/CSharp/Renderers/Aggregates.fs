namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Aggregates =

    open type SyntaxFactory
    open Statements
    open PacketParts

    // ----- whole-protocol aggregates: registry tables, dispatcher, handler base -----

    /// Validate + format a hand-assembled aggregate source file (several top-level types per
    /// file, so the single-decl `renderUnit` does not fit). Same hard gate: parse errors fail
    /// generation instead of writing the file.
    let private renderRawUnit (label: string) (text: string) : string =
        let cu = ParseCompilationUnit text

        let errors =
            cu.GetDiagnostics()
            |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
            |> Seq.toList

        if not errors.IsEmpty then
            failwithf
                "codegen: emitted invalid C# for %s:\n%s\n----\n%s"
                label
                (errors |> List.map string |> String.concat "\n")
                text

        Text.blankLineAfterNamespace (cu.NormalizeWhitespace("    ", "\n", false).ToFullString())
        + "\n"

    let private allStates = [ Handshaking; Status; Login; Configuration; Play ]
    let private allDirs = [ Clientbound; Serverbound ]

    /// A packet whose read side is not fully generated for at least one layout. Excluded from
    /// dispatch and the handler base: its ordinal falls through to `Unknown`, so a stub `Read`
    /// throw can never take down the receive loop.
    let private hasReadStub (s: RuntimeSurface) (p: PacketSpec) =
        p.Layouts |> List.exists (fun l -> readEntriesLines s l.Entries |> hasReadError)

    let rec private wireNamedRefs (w: WireType) : string list =
        match w with
        | Named n -> [ n ]
        | EnumRef(n, _) -> [ n ]
        | Array(item, cnt) ->
            wireNamedRefs item
            @ (match cnt with
               | TypedCount t -> wireNamedRefs t
               | _ -> [])
        | Option inner -> wireNamedRefs inner
        | RegistryHolder inner -> wireNamedRefs inner
        | SentinelArray(item, _) -> wireNamedRefs item
        | Switch(_, cases) -> cases |> List.collect (fun c -> wireNamedRefs c.Type)
        | _ -> []

    let rec private entryNamedRefs (e: WireEntry) : string list =
        match e with
        | Read(_, w, _) -> wireNamedRefs w
        | Discard(_, w) -> wireNamedRefs w
        | IfNonZero(_, inner) -> inner |> List.collect entryNamedRefs
        | ReadOpt(_, w, _, _, _) -> wireNamedRefs w
        | ReadBlock(w, _, inner) -> wireNamedRefs w @ (inner |> List.collect entryNamedRefs)
        | ReadUnion(_, u, _) -> [ u ]
        | InlineUnion(_, arms) -> arms |> List.collect (fun a -> a.Entries |> List.collect entryNamedRefs)

    let rec private apiNamedRefs (t: ApiType) : string list =
        match t with
        | TArray inner
        | TOption inner
        | THolder inner -> apiNamedRefs inner
        | TNamed n -> [ n ]
        | TUnion n -> [ n ]
        | TEnum n -> [ n ]
        | _ -> []

    /// Named types the delivered output can resolve: runtime-provided primitives plus generated
    /// named/bitflags/union types that are themselves stub-free and reference only resolvable
    /// types (fixpoint). A union is a candidate like any other type — it is generated now, so a
    /// union reference resolves as soon as every type its arms read does.
    let private resolvableTypes (s: RuntimeSurface) (protocol: ProtocolSpec) : Set<string> =
        let runtimeProvided = Set.ofList [ "Position" ]

        let stubFreeEntries (entries: WireEntry list) =
            readEntriesLines s entries |> hasReadError |> not

        let candidates =
            [
                for t in protocol.Types ->
                    let refs =
                        (t.ApiFields |> List.collect (fun f -> apiNamedRefs f.Type))
                        @ (t.Layouts |> List.collect (fun l -> l.Entries |> List.collect entryNamedRefs))

                    t.Name, refs, t.Layouts |> List.forall (fun l -> stubFreeEntries l.Entries)
                for u in protocol.Unions ->
                    let arms = u.Layouts |> List.collect (fun l -> l.Arms)

                    let refs = arms |> List.collect (fun a -> a.Entries |> List.collect entryNamedRefs)

                    u.Name, refs, arms |> List.forall (fun a -> stubFreeEntries a.Entries)
            ]

        let mutable known =
            Set.unionMany
                [
                    runtimeProvided
                    protocol.Bitflags |> List.map (fun b -> b.Name) |> Set.ofList
                    protocol.Enums |> List.map (fun e -> e.Name) |> Set.ofList
                ]

        let mutable changed = true

        while changed do
            changed <- false

            for name, refs, ok in candidates do
                if ok && not (known.Contains name) && refs |> List.forall known.Contains then
                    known <- known.Add name
                    changed <- true

        known

    /// A packet the dispatcher may reference: read side fully generated AND every named type it
    /// touches resolvable in the delivered output (mirrors the delivery exclusions by data,
    /// not by file name).
    let private isDispatchable (s: RuntimeSurface) (known: Set<string>) (p: PacketSpec) =
        let refs =
            (p.ApiFields |> List.collect (fun f -> apiNamedRefs f.Type))
            @ (p.Layouts |> List.collect (fun l -> l.Entries |> List.collect entryNamedRefs))

        not (hasReadStub s p) && refs |> List.forall known.Contains

    /// Packet type name relative to the root namespace (`Packets.Play.Clientbound.KeepAlivePacket`).
    let private relTypeName (p: PacketSpec) =
        sprintf "Packets.%A.%A.%s" p.State p.Direction p.ClassName

    let private shortName (p: PacketSpec) =
        if p.ClassName.EndsWith "Packet" then
            p.ClassName.[.. p.ClassName.Length - 7]
        else
            p.ClassName

    /// `Flow/PacketRegistry.g.cs`: descriptors + dense id->ordinal tables per pv-run.
    let private renderRegistryFile (s: RuntimeSurface) (entries: Registry.CatalogEntry list) : string =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    for dir in allDirs do
                        let slice = Registry.slice st dir entries

                        if not slice.IsEmpty then
                            yield st, dir, slice
            ]

        // pv runs with an identical id -> ordinal layout, per slice
        let runsFor (slice: Registry.CatalogEntry list) =
            let ids = slice |> List.collect (fun e -> e.Spec.Ids)

            if ids.IsEmpty then
                []
            else
                let minPv = ids |> List.map (fun (lo, _, _) -> lo) |> List.min
                let maxPv = ids |> List.map (fun (_, hi, _) -> hi) |> List.max

                let mapAt pv =
                    [
                        for e in slice do
                            for lo, hi, id in e.Spec.Ids do
                                if pv >= lo && pv <= hi then
                                    yield id, e.Ordinal
                    ]
                    |> List.sortBy fst

                let mutable runs = []
                let mutable runLo = minPv
                let mutable current = mapAt minPv

                for pv in minPv + 1 .. maxPv do
                    let m = mapAt pv

                    if m <> current then
                        runs <- (runLo, pv - 1, current) :: runs
                        runLo <- pv
                        current <- m

                runs <- (runLo, maxPv, current) :: runs
                runs |> List.rev |> List.filter (fun (_, _, m) -> not (List.isEmpty m))

        line "using System;"
        line "using System.Diagnostics.CodeAnalysis;"
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line "public readonly record struct IdRange(int FromPv, int ToPv, int Id);"
        line ""
        line (sprintf "public sealed record PacketDescriptor(%s Identity, IdRange[] Ids);" s.IdentityType)
        line ""
        line "/// <summary>Generated packet registry: dense id->ordinal tables on the hot path,"
        line "/// descriptor catalogs on the cold one. Unknown ids are a normal stream condition:"
        line "/// every entry point is Try.</summary>"
        line "public static partial class PacketRegistry"
        line "{"

        for st, dir, slice in slices do
            line (sprintf "    private static readonly PacketDescriptor[] Catalog%A%A =" st dir)
            line "    ["

            for e in slice do
                let ids =
                    coalesceIds e.Spec.Ids
                    |> List.map (fun (lo, hi, id) -> sprintf "new(%d, %d, 0x%02X)" lo hi id)
                    |> String.concat ", "

                // identity inlined (same data the packet's own Identity is printed from): the
                // registry must not reference packet types — some are not deliverable yet.
                let identity =
                    sprintf
                        "new(\"%s\", \"%s\", %s.%A, %s.%A, %d)"
                        e.Key
                        (shortName e.Spec)
                        s.PhaseEnum
                        e.Spec.State
                        s.DirectionEnum
                        e.Spec.Direction
                        e.Ordinal

                line (sprintf "        new(%s, [%s])," identity ids)

            line "    ];"
            line ""

        // ----- the flat lookup: every per-run table concatenated once, addressed by arithmetic -----
        // The tables above are keyed by (phase, direction) and a protocol-version *range*, which a
        // switch plus a chain of range compares can answer but only in time proportional to the
        // number of ranges. Concatenating them into one blob and indexing it by
        // ((int)phase * directions + (int)direction) * pvCount + (pv - minPv) answers the same
        // question with two loads and no branching on the version at all.
        let dirCount = s.DirectionOrder.Length
        let slotCount = s.PhaseOrder.Length * dirCount

        let slotOf (st: ProtocolState) (dir: Direction) =
            let phaseIndex = s.PhaseOrder |> List.findIndex ((=) st)
            let dirIndex = s.DirectionOrder |> List.findIndex ((=) dir)
            phaseIndex * dirCount + dirIndex

        let allRuns =
            [
                for st, dir, slice in slices do
                    for lo, hi, map in runsFor slice do
                        yield st, dir, lo, hi, map
            ]

        let minPv = allRuns |> List.map (fun (_, _, lo, _, _) -> lo) |> List.min
        let maxPv = allRuns |> List.map (fun (_, _, _, hi, _) -> hi) |> List.max
        let pvCount = maxPv - minPv + 1

        let blob = ResizeArray<int>()
        let offsets = Array.zeroCreate<int> (slotCount * pvCount)
        let lengths = Array.zeroCreate<int> (slotCount * pvCount)

        for st, dir, lo, hi, map in allRuns do
            let maxId = map |> List.map fst |> List.max
            let table = Array.create (maxId + 1) -1

            for id, ordinal in map do
                table.[id] <- ordinal

            let offset = blob.Count
            blob.AddRange table

            for pv in lo..hi do
                let index = slotOf st dir * pvCount + (pv - minPv)
                offsets.[index] <- offset
                lengths.[index] <- table.Length

        let window =
            [
                for i in 0 .. offsets.Length - 1 do
                    yield offsets.[i]
                    yield lengths.[i]
            ]

        line "    /// <summary>Number of members of the phase and direction enums the tables were built"
        line "    /// against. Public because a caller that indexes anything by (phase, direction) must"
        line "    /// size it from the same numbers rather than reflecting over the enums.</summary>"
        line (sprintf "    public const int PhaseCount = %d;" s.PhaseOrder.Length)
        line ""
        line (sprintf "    public const int DirectionCount = %d;" dirCount)
        line ""
        line (sprintf "    public const int CatalogCount = %d;" slotCount)
        line ""
        line (sprintf "    private const int MinPv = %d;" minPv)
        line ""
        line (sprintf "    private const int PvCount = %d;" pvCount)
        line ""
        line "    /// <summary>Every per-run id->ordinal table, concatenated. -1 marks an id this run"
        line "    /// does not map.</summary>"
        line "    private static ReadOnlySpan<short> OrdinalBlob =>"
        line (sprintf "        [%s];" (blob |> Seq.map string |> String.concat ", "))
        line ""
        line "    /// <summary>Offset and length, interleaved, of the table for one (phase, direction,"
        line "    /// protocol version) inside <see cref=\"OrdinalBlob\"/>: the pair sits in one cache line"
        line "    /// so the lookup reads both with a single probe. Length 0 means that combination"
        line "    /// carries no packets.</summary>"
        line "    private static ReadOnlySpan<int> TableWindow =>"
        line (sprintf "        [%s];" (window |> Seq.map string |> String.concat ", "))
        line ""

        line (
            sprintf
                "    public static bool TryGetOrdinal(int id, int %s, %s phase, %s dir, out ushort ordinal)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "    {"
        line (sprintf "        var pvIndex = %s - MinPv;" s.VersionParam)
        line "        // phase and direction are bounded separately on purpose: a single check on the"
        line "        // combined slot would let an out-of-range direction alias onto another phase's row."
        line "        if ((uint)pvIndex < PvCount && (uint)phase < PhaseCount && (uint)dir < DirectionCount)"
        line "        {"
        line "            var window = (((int)phase * DirectionCount + (int)dir) * PvCount + pvIndex) * 2;"
        line "            if ((uint)id < (uint)TableWindow[window + 1])"
        line "            {"
        line "                var value = OrdinalBlob[TableWindow[window] + id];"
        line "                if (value >= 0)"
        line "                {"
        line "                    ordinal = (ushort)value;"
        line "                    return true;"
        line "                }"
        line "            }"
        line "        }"
        line ""
        line "        ordinal = 0;"
        line "        return false;"
        line "    }"
        line ""

        line (
            sprintf
                "    public static bool TryResolve(int id, int %s, %s phase, %s dir, [NotNullWhen(true)] out PacketDescriptor? descriptor)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "    {"
        line (sprintf "        if (TryGetOrdinal(id, %s, phase, dir, out var ordinal))" s.VersionParam)
        line "        {"
        line "            descriptor = Catalog(phase, dir)[ordinal];"
        line "            return true;"
        line "        }"
        line ""
        line "        descriptor = null;"
        line "        return false;"
        line "    }"
        line ""

        line "    /// <summary>Gets the wire id a packet carries on the specified protocol version, or"
        line "    /// false when the packet does not exist there. The reverse of TryGetOrdinal: the dense"
        line "    /// tables index id-&gt;ordinal, so one packet's own ranges are scanned instead, which is"
        line "    /// what the send path needs and it is cold.</summary>"

        line (
            sprintf
                "    public static bool TryGetId(in %s identity, int %s, out int id)"
                s.IdentityType
                s.VersionParam
        )

        line "    {"
        line "        foreach (var range in Catalog(identity.Phase, identity.Direction)[identity.Ordinal].Ids)"
        line "        {"
        line (sprintf "            if (%s >= range.FromPv && %s <= range.ToPv)" s.VersionParam s.VersionParam)
        line "            {"
        line "                id = range.Id;"
        line "                return true;"
        line "            }"
        line "        }"
        line ""
        line "        id = 0;"
        line "        return false;"
        line "    }"
        line ""

        line "    /// <summary>Gets the wire id a packet carries on the specified protocol version.</summary>"

        line (
            sprintf "    public static int GetId(in %s identity, int %s)" s.IdentityType s.VersionParam
        )

        line "    {"
        line (sprintf "        if (TryGetId(identity, %s, out var id)) return id;" s.VersionParam)

        line (
            sprintf
                "        throw new System.NotSupportedException($\"No packet id for protocol {%s}.\");"
                s.VersionParam
        )

        line "    }"
        line ""

        line (
            sprintf
                "    public static ReadOnlySpan<PacketDescriptor> Catalog(%s phase, %s dir)"
                s.PhaseEnum
                s.DirectionEnum
        )

        line "    {"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (
                sprintf "            case (%s.%A, %s.%A): return Catalog%A%A;" s.PhaseEnum st s.DirectionEnum dir st dir
            )

        line "        }"
        line ""
        line "        return default;"
        line "    }"
        line "}"
        sb.ToString()

    /// `Flow/PacketFlow.g.cs`: one lookup + one ordinal jump table + one constrained call per
    /// packet. Dispatch is deliberately synchronous: the decode must finish before the next
    /// transport read (the `IncomingPacket.Body` window); anything async happens in the facade after.
    let private renderFlowFile
        (s: RuntimeSurface)
        (dispatchable: PacketSpec -> bool)
        (entries: Registry.CatalogEntry list)
        : string
        =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    for dir in allDirs do
                        let slice =
                            Registry.slice st dir entries |> List.filter (fun e -> dispatchable e.Spec)

                        if not slice.IsEmpty then
                            yield st, dir, slice
            ]

        // A `.g.cs` file is auto-generated as far as the compiler is concerned, so the project's
        // `<Nullable>enable</Nullable>` does not reach it and every `?` here would be both a
        // CS8669 warning and a dead annotation. The Try doors depend on the annotation being
        // live: `[NotNullWhen(true)] out IPacket?` is what makes a caller who reads the packet
        // after a false return get a warning instead of a null.
        line "#nullable enable"
        line ""
        line (sprintf "using %s;" s.UsingSystem)
        line (sprintf "using %s;" s.UsingSerialization)
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""

        line (
            sprintf "public delegate void TrailingBytesHook(int packetId, int %s, long remainingBytes);" s.VersionParam
        )

        line ""
        line "/// <summary>Generated dispatcher. Packets whose codegen is still stubbed are not"
        line "/// dispatched — they fall through to <c>Unknown</c> instead of throwing inside the"
        line "/// receive loop. Trailing bytes raise a hook, not an exception: the packet already"
        line "/// reached the visitor, but the spec is suspect."
        line "/// Three doors onto the same table: <c>Dispatch</c> (throws on a malformed body),"
        line "/// <c>TryDispatch</c> (same visitor, a false + reason instead of the throw) and"
        line "/// <c>TryDecode</c> (visitor-free — hands back the decoded packet itself).</summary>"
        line "public static partial class PacketFlow"
        line "{"
        line "    public static event TrailingBytesHook? OnTrailingBytes;"
        line ""
        line "    /// <summary>Raises <see cref=\"OnTrailingBytes\"/> for a caller that decoded the body"
        line "    /// itself. An event can only be raised inside the type that declares it, and the"
        line "    /// generated handlers decode without going through <see cref=\"Dispatch\"/>; the hook"
        line "    /// stays the one place a suspect spec is reported from.</summary>"

        line (
            sprintf
                "    internal static void RaiseTrailingBytes(int packetId, int %s, long remainingBytes) => OnTrailingBytes?.Invoke(packetId, %s, remainingBytes);"
                s.VersionParam
                s.VersionParam
        )

        line ""

        line (
            sprintf
                "    public static void Dispatch<TVisitor>(in IncomingPacket raw, int %s, %s phase, %s dir, ref TVisitor visitor)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "        where TVisitor : IPacketVisitor"
        line "    {"

        line (
            sprintf "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, dir, out var ordinal))" s.VersionParam
        )

        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return;"
        line "        }"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        bool handled;"
        line "        // The jump table is shared with the Try door, which must tell a failed body read"
        line "        // from an exception thrown by the visitor: the table lowers this flag once the"
        line "        // body is decoded, right before it calls the visitor. Dispatch converts nothing,"
        line "        // so here the flag is written and never read."
        line "        bool reading = true;"
        line "        switch (phase, dir)"
        line "        {"

        for st, dir, _ in slices do
            line (sprintf "            case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)

            line (
                sprintf
                    "                handled = Dispatch%A%A(ordinal, ref %s, %s, ref visitor, ref reading);"
                    st
                    dir
                    s.ReaderParam
                    s.VersionParam
            )

            line "                break;"

        line "            default:"
        line "                handled = false;"
        line "                break;"
        line "        }"
        line ""
        line "        if (!handled)"
        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return;"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)

        line (
            sprintf "            OnTrailingBytes?.Invoke(raw.Id, %s, %s.RemainingCount);" s.VersionParam s.ReaderParam
        )

        line "    }"

        // ----- TryDispatch: the same table, a reason instead of a throw -----
        line ""
        line "    /// <summary>Dispatch that survives a malformed body: returns false with"
        line "    /// <paramref name=\"error\" /> filled where <see cref=\"Dispatch\" /> would let the"
        line "    /// exception out. True means the packet reached the visitor — including the normal"
        line "    /// stream condition of an id this (phase, direction) has no mapping for, which"
        line "    /// reaches <c>Unknown</c> exactly as in <see cref=\"Dispatch\" />. Trailing bytes stay"
        line "    /// a hook, not a failure. Only a failure of the body read is converted, and only the"
        line "    /// kinds a decoder may swallow (see <c>TryClassify</c>): cancellation, a stubbed"
        line "    /// decoder and out-of-memory still propagate. An exception thrown by the visitor"
        line "    /// itself is never converted — the table lowers <c>reading</c> before it calls the"
        line "    /// visitor, so the consumer's own bugs come out as themselves.</summary>"

        line (
            sprintf
                "    public static bool TryDispatch<TVisitor>(in IncomingPacket raw, int %s, %s phase, %s direction, ref TVisitor visitor, out DecodeError error)"
                s.VersionParam
                s.PhaseEnum
                s.DirectionEnum
        )

        line "        where TVisitor : IPacketVisitor"
        line "    {"
        line "        error = DecodeError.None;"
        line ""

        line (
            sprintf
                "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, direction, out var ordinal))"
                s.VersionParam
        )

        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return true;"
        line "        }"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        bool handled;"
        line "        // True while the body is being read; the table lowers it right before it hands the"
        line "        // packet to the visitor. The filter below tests it, so an exception out of the"
        line "        // visitor is not mistaken for a malformed packet."
        line "        bool reading = true;"
        line "        try"
        line "        {"
        line "            switch (phase, direction)"
        line "            {"

        for st, dir, _ in slices do
            line (sprintf "                case (%s.%A, %s.%A):" s.PhaseEnum st s.DirectionEnum dir)

            line (
                sprintf
                    "                    handled = Dispatch%A%A(ordinal, ref %s, %s, ref visitor, ref reading);"
                    st
                    dir
                    s.ReaderParam
                    s.VersionParam
            )

            line "                    break;"

        line "                default:"
        line "                    handled = false;"
        line "                    break;"
        line "            }"
        line "        }"
        line "        catch (Exception ex) when (reading && TryClassify(ex, out var reason))"
        line "        {"
        line "            error = reason;"
        line "            return false;"
        line "        }"
        line ""
        line "        if (!handled)"
        line "        {"
        line "            visitor.Unknown(in raw);"
        line "            return true;"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)

        line (
            sprintf "            OnTrailingBytes?.Invoke(raw.Id, %s, %s.RemainingCount);" s.VersionParam s.ReaderParam
        )

        line ""
        line "        return true;"
        line "    }"

        // ----- TryDecode: the visitor-free door, only when packets carry a non-generic type -----
        match s.PacketBaseInterface with
        | Some baseIface ->
            line ""
            line "    /// <summary>One raw packet in, one decoded packet out — no visitor to write."
            line (sprintf "    /// An id this (phase, direction) cannot map yields an <see cref=\"UnknownPacket\" />")
            line "    /// and still returns true: an unmapped id is a normal stream condition, not an error."
            line "    /// A malformed body returns false with <paramref name=\"error\" /> filled and"
            line "    /// <paramref name=\"packet\" /> null. The allocation-free hot path is"
            line "    /// <see cref=\"Dispatch\" /> / <see cref=\"TryDispatch\" />; this door costs nothing"
            line "    /// extra either — packets are classes, so the capture is a reference, not a box.</summary>"

            line (
                sprintf
                    "    public static bool TryDecode(in IncomingPacket raw, int %s, %s phase, %s direction, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out %s? packet, out DecodeError error)"
                    s.VersionParam
                    s.PhaseEnum
                    s.DirectionEnum
                    baseIface
            )

            line "    {"
            line "        var capture = new Capture(phase, direction);"
            line ""

            line (
                sprintf "        if (!TryDispatch(in raw, %s, phase, direction, ref capture, out error))" s.VersionParam
            )

            line "        {"
            line "            packet = null;"
            line "            return false;"
            line "        }"
            line ""
            line "        packet = capture.Result!;"
            line "        return true;"
            line "    }"
            line ""

            line (
                sprintf
                    "    /// <summary>Keeps the decoded packet as <see cref=\"%s\" />. The assignment is a"
                    baseIface
            )

            line "    /// reference conversion (every packet is a class that implements it), so there is no"
            line "    /// boxing and no adapter object — the same jump table, one field write.</summary>"
            line "    private struct Capture : IPacketVisitor"
            line "    {"
            line (sprintf "        private readonly %s _phase;" s.PhaseEnum)
            line (sprintf "        private readonly %s _direction;" s.DirectionEnum)
            line ""
            line (sprintf "        public %s? Result;" baseIface)
            line ""
            line (sprintf "        public Capture(%s phase, %s direction)" s.PhaseEnum s.DirectionEnum)
            line "        {"
            line "            _phase = phase;"
            line "            _direction = direction;"
            line "            Result = null;"
            line "        }"
            line ""

            line (
                sprintf
                    "        public void Visit<T>(T packet) where T : class, %s<T> => Result = (%s)packet;"
                    (s.PacketInterface |> Option.defaultValue baseIface)
                    baseIface
            )

            line ""

            line
                "        public void Unknown(in IncomingPacket raw) => Result = new UnknownPacket(raw.Id, _phase, _direction);"

            line "    }"
        | None -> ()

        // ----- the one place that decides what a Try door is allowed to swallow -----
        line ""
        line "    /// <summary>Maps an exception raised while reading a packet body onto a"
        line "    /// <see cref=\"DecodeError\" />. Returns false for exceptions a decoder must never"
        line "    /// swallow — used as an exception filter, so those propagate without unwinding."
        line "    /// <para>"
        line "    /// <c>ArgumentException</c> is deliberately NOT on the propagate list. Bytes off the"
        line "    /// wire reach it: a compound with a duplicate key ends in <c>Dictionary.Add</c> inside"
        line "    /// <c>NbtCompound.Add</c>, and an unnamed tag in a compound throws there too. Those are"
        line "    /// data errors, so they are <c>Malformed</c>. Only an exception the caller's own code"
        line "    /// raised should escape a Try door, and that case is handled before the filter runs:"
        line "    /// the jump table lowers <c>reading</c> before it calls the visitor."
        line "    /// </para></summary>"
        line "    private static bool TryClassify(Exception ex, out DecodeError error)"
        line "    {"
        line "        switch (ex)"
        line "        {"
        line "            case ProtocolNotSupportException _:"
        line "            case NotSupportedException _:"
        line "                error = DecodeError.UnsupportedVersion;"
        line "                return true;"
        line "            case OperationCanceledException _:"
        line "            case NotImplementedException _:"
        line "            case OutOfMemoryException _:"
        line "                error = DecodeError.None;"
        line "                return false;"
        line "            default:"
        line "                error = DecodeError.Malformed;"
        line "                return true;"
        line "        }"
        line "    }"

        for st, dir, slice in slices do
            line ""
            line (sprintf "    /// <summary>One ordinal, one read, one constrained call. <paramref name=\"reading\" />")
            line "    /// goes false between the two: above it the exception is the packet's fault, below it"
            line "    /// the visitor's. Only the Try door reads it.</summary>"

            line (
                sprintf
                    "    private static bool Dispatch%A%A<TVisitor>(ushort ordinal, ref %s %s, int %s, ref TVisitor visitor, ref bool reading)"
                    st
                    dir
                    s.ReaderType
                    s.ReaderParam
                    s.VersionParam
            )

            line "        where TVisitor : IPacketVisitor"
            line "    {"
            line "        switch (ordinal)"
            line "        {"

            for e in slice do
                line (sprintf "            case %d:" e.Ordinal)
                line "            {"

                line (
                    sprintf
                        "                var packet = %s.Read(ref %s, %s);"
                        (relTypeName e.Spec)
                        s.ReaderParam
                        s.VersionParam
                )

                line "                reading = false;"
                line "                visitor.Visit(packet);"
                line "                return true;"
                line "            }"
                line ""

            line "            default:"
            line "                return false;"
            line "        }"
            line "    }"

        line "}"
        sb.ToString()

    /// Handler method name: bare `On{Short}` while unique across clientbound; on a collision
    /// Play keeps the bare name and other phases get a phase prefix.
    let private handlerNames (entries: Registry.CatalogEntry list) : Map<string * string, string> =
        let counts =
            entries |> List.map (fun e -> shortName e.Spec) |> List.countBy id |> Map.ofList

        entries
        |> List.map (fun e ->
            let short = shortName e.Spec

            let name =
                if counts.[short] = 1 || e.Spec.State = Play then
                    sprintf "On%s" short
                else
                    sprintf "On%A%s" e.Spec.State short

            (sprintf "%A" e.Spec.State, e.Spec.ClassName), name)
        |> Map.ofList

    /// `Flow/<Direction>Handler.g.cs`: one base for every phase of one direction, phase slot led
    /// by the consumer, ValueTask handlers awaited by the facade after the synchronous dispatch.
    ///
    /// `HandleAsync` resolves the ordinal itself and reads the packet inside the case block that
    /// already knows its type, so the decoded packet reaches `On<Name>` with nothing dynamic in
    /// between. The handler is deliberately NOT an `IPacketVisitor`: routing it through
    /// `PacketFlow` would hand the visitor's `Visit<T>` a `ValueTask` it has nowhere to return,
    /// and a dropped `ValueTask` is a silently lost continuation. `PacketFlow` and
    /// `IPacketVisitor` remain for callers that want them — `PacketSubscriptions` is one — and
    /// are emitted unchanged.
    let private renderHandlerFile
        (s: RuntimeSurface)
        (dispatchable: PacketSpec -> bool)
        (dir: Direction)
        (className: string)
        (entries: Registry.CatalogEntry list)
        : string
        =
        let sb = System.Text.StringBuilder()
        let line (t: string) = sb.AppendLine t |> ignore

        let slices =
            [
                for st in allStates do
                    let slice =
                        Registry.slice st dir entries |> List.filter (fun e -> dispatchable e.Spec)

                    if not slice.IsEmpty then
                        yield st, slice
            ]

        let names = handlerNames (slices |> List.collect snd)

        // The phase a connection is in when the first packet of this direction can arrive: a
        // client starts listening in Login, a server starts reading in Handshaking.
        let defaultPhase =
            match dir with
            | Clientbound -> "Login"
            | Serverbound -> "Handshaking"

        let lowerDir = (sprintf "%A" dir).ToLowerInvariant()

        line "using System.Threading.Tasks;"
        line (sprintf "using %s;" s.UsingSerialization)
        line ""
        line (sprintf "namespace %s;" s.Namespace)
        line ""
        line (sprintf "/// <summary>Generated handler base over every %s phase. The truth about" lowerDir)
        line "/// the current phase is the consumer's: set <see cref=\"Phase\"/> as the connection"
        line "/// advances. <c>HandleAsync</c> decodes synchronously (the raw data window must not"
        line "/// cross an await) and awaits the handler's result after. <c>OnUnknown</c> must not"
        line "/// hold on to <c>raw</c> beyond the call.</summary>"
        line (sprintf "public abstract partial class %s" className)
        line "{"
        line (sprintf "    public %s Phase { get; protected set; } = %s.%s;" s.PhaseEnum s.PhaseEnum defaultPhase)
        line ""
        line (sprintf "    protected static %s Direction => %s.%A;" s.DirectionEnum s.DirectionEnum dir)
        line ""

        line "    /// <summary>The registry lookup and the typed read happen here, in a case block where"
        line "    /// the packet type is statically known, so nothing between the wire and"
        line "    /// <c>On&lt;Name&gt;</c> is dynamic. <see cref=\"Phase\"/> is read once: a handler that"
        line "    /// advances the phase does so after the switch, and this packet is read as the phase"
        line "    /// it arrived in.</summary>"
        line (sprintf "    public ValueTask HandleAsync(in IncomingPacket raw, int %s)" s.VersionParam)
        line "    {"
        line "        var phase = Phase;"

        line (
            sprintf
                "        if (!PacketRegistry.TryGetOrdinal(raw.Id, %s, phase, %s.%A, out var ordinal))"
                s.VersionParam
                s.DirectionEnum
                dir
        )

        line "            return OnUnknown(in raw);"
        line ""
        line (sprintf "        var %s = new %s(raw.Body);" s.ReaderParam s.ReaderType)
        line "        ValueTask pending;"
        line "        switch (phase)"
        line "        {"

        for st, slice in slices do
            line (sprintf "            case %s.%A:" s.PhaseEnum st)
            line "                switch (ordinal)"
            line "                {"

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line (sprintf "                    case %d:" e.Ordinal)
                line "                    {"

                line (
                    sprintf
                        "                        var packet = %s.Read(ref %s, %s);"
                        (relTypeName e.Spec)
                        s.ReaderParam
                        s.VersionParam
                )

                line (sprintf "                        pending = %s(packet);" handler)
                line "                        break;"
                line "                    }"

            line ""
            line "                    default:"
            line "                        return OnUnknown(in raw);"
            line "                }"
            line ""
            line "                break;"

        line "            default:"
        line "                return OnUnknown(in raw);"
        line "        }"
        line ""
        line (sprintf "        if (%s.RemainingCount != 0)" s.ReaderParam)

        line (
            sprintf
                "            PacketFlow.RaiseTrailingBytes(raw.Id, %s, %s.RemainingCount);"
                s.VersionParam
                s.ReaderParam
        )

        line ""
        line "        return pending;"
        line "    }"

        line ""
        line "    protected virtual ValueTask OnUnknown(in IncomingPacket raw) => default;"

        for st, slice in slices do
            line ""
            line (sprintf "    // --- %A ---" st)

            for e in slice do
                let handler = names.[(sprintf "%A" st, e.Spec.ClassName)]

                line ""
                line (sprintf "    protected virtual ValueTask %s(%s packet) => default;" handler (relTypeName e.Spec))

        line "}"
        sb.ToString()

    /// Aggregate outputs under `Flow/`. Excluded from the sandbox (they need the real transport
    /// types); their real test is the McProtoNet build after delivery. The registry covers the
    /// whole catalog (identities inlined); dispatcher and handler reference only packets that
    /// are stub-free and whose named types are resolvable in the delivered output.
    let internal renderProtocolExtras (s: RuntimeSurface) (protocol: ProtocolSpec) : GeneratedFile list =
        let entries = Registry.catalog protocol.Packets
        let known = resolvableTypes s protocol
        let dispatchable (p: PacketSpec) = isDispatchable s known p

        [
            {
                RelativePath = "Flow/PacketRegistry.g.cs"
                Contents = renderRawUnit "PacketRegistry" (renderRegistryFile s entries)
            }
            {
                RelativePath = "Flow/PacketFlow.g.cs"
                Contents = renderRawUnit "PacketFlow" (renderFlowFile s dispatchable entries)
            }
            {
                RelativePath = "Flow/ClientboundHandler.g.cs"
                Contents =
                    renderRawUnit
                        "ClientboundHandler"
                        (renderHandlerFile s dispatchable Clientbound "ClientboundHandler" entries)
            }
            {
                RelativePath = "Flow/ServerboundHandler.g.cs"
                Contents =
                    renderRawUnit
                        "ServerboundHandler"
                        (renderHandlerFile s dispatchable Serverbound "ServerboundHandler" entries)
            }
        ]
