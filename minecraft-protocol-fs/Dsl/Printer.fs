namespace McProtocol.Dsl

/// Pretty-printer for a ProtocolSpec — the current validation surface
/// (`dotnet run` dumps the whole protocol). Kept separate from the DSL
/// definitions so a future C# renderer can sit alongside it.
module Printer =

    let printField indent (f: ApiField) =
        let pad = String.replicate indent "  "
        printfn "%s%-20s : %-24A [%A]" pad f.Name f.Type f.Present

    let rec printEntry indent (entry: WireEntry) =
        let pad = String.replicate indent "  "

        match entry with
        | Read(wire, t, api) ->
            printfn "%sread %-20s -> %-20s [%A]" pad wire api t

        | Discard(wire, t) ->
            printfn "%sdiscard %-17s [%A]" pad wire t

        | IfNonZero(field, entries) ->
            printfn "%sif %s != 0:" pad field
            for e in entries do
                printEntry (indent + 1) e

        | ReadOpt(wire, t, api, disc, keys) ->
            printfn "%sreadOpt %-17s -> %-20s when %s in %A [%A]" pad wire api disc keys t

        | ReadBlock(typ, api, entries) ->
            printfn "%sreadBlock -> %-20s [%A]" pad api typ
            for e in entries do
                printEntry (indent + 1) e

        | ReadUnion(disc, unionName, api) ->
            printfn "%sreadUnion %s -> %s : %s" pad disc api unionName

        | InlineUnion(disc, arms) ->
            printfn "%sinlineUnion(%s):" pad disc
            for a in arms do
                printfn "%s  [%A] -> %s" pad a.Keys a.Name
                for e in a.Entries do
                    printEntry (indent + 2) e

    let printNamedType (spec: NamedTypeSpec) =
        printfn "=== type %s ===" spec.Name

        printfn "API:"
        for f in spec.ApiFields do
            printField 1 f

        for l in spec.Layouts do
            printfn "Wire [%A]:" l.Range
            for e in l.Entries do
                printEntry 1 e

        printfn ""

    let printUnion (spec: UnionTypeSpec) =
        printfn "=== union %s ===" spec.Name

        for l in spec.Layouts do
            printfn "Cases [%A]:" l.Range
            for a in l.Arms do
                printfn "  [%A] -> %s" a.Keys a.Name
                for e in a.Entries do
                    printEntry 2 e

        printfn ""

    let printBitflags (spec: BitflagsSpec) =
        printfn "=== bitflags %s ===" spec.Name
        for l in spec.Layouts do
            printfn "Flags [%A] (%A): %s" l.Range l.Backing (String.concat ", " l.Flags)
        printfn ""

    let printPacket (spec: PacketSpec) =
        printfn "=== packet %s | %A %A | %A ===" spec.ClassName spec.State spec.Direction spec.Since

        printfn "API:"
        for f in spec.ApiFields do
            printField 1 f

        for l in spec.Layouts do
            printfn "Wire [%A]:" l.Range
            for e in l.Entries do
                printEntry 1 e

        printfn ""

    let printProtocol (spec: ProtocolSpec) =
        printfn "===== NAMED TYPES ====="
        for t in spec.Types do
            printNamedType t

        printfn "===== UNIONS ====="
        for u in spec.Unions do
            printUnion u

        printfn "===== BITFLAGS ====="
        for b in spec.Bitflags do
            printBitflags b

        printfn "===== PACKETS ====="
        for p in spec.Packets do
            printPacket p
