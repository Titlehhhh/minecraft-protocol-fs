namespace McProtocol.Codegen

open System.IO
open System.Text
open System.Text.Json
open McProtocol.Dsl

/// Multiversion coverage map: crosses the packet universe from the McProtoFacts-derived
/// manifest (`Spec/protocol-ids.json`) with the packet specs actually present in the
/// protocol (`Spec/Packets/**`). Output is a regenerable markdown backlog — done / wire
/// gaps / stubs / missing — per release protocol version. See `dotnet run -- coverage`.
module Coverage =

    /// Release protocol versions the library targets (735–772), with display names.
    /// Mirrors McProtoNet's `MinecraftVersion` table; snapshot/pre/rc protocols
    /// (737–750, 752) are left out on purpose — nobody ships against them.
    let knownVersions =
        [
            735, "1.16"
            736, "1.16.1"
            751, "1.16.2"
            753, "1.16.3"
            754, "1.16.4–1.16.5"
            755, "1.17"
            756, "1.17.1"
            757, "1.18–1.18.1"
            758, "1.18.2"
            759, "1.19"
            760, "1.19.2"
            761, "1.19.3"
            762, "1.19.4"
            763, "1.20–1.20.1"
            764, "1.20.2"
            765, "1.20.3–1.20.4"
            766, "1.20.5–1.20.6"
            767, "1.21–1.21.1"
            768, "1.21.3"
            769, "1.21.4"
            770, "1.21.5"
            771, "1.21.6"
            772, "1.21.7–1.21.8"
        ]

    let private contains pv range =
        let lo, hi = VersionRangeX.bounds range
        (lo |> Option.forall (fun l -> pv >= l)) && (hi |> Option.forall (fun h -> pv <= h))

    /// Compress a set of pvs into "a, b–c" chunks. Adjacency follows the known-version
    /// ordering, not integer succession (753 and 754 are neighbours; so are 736 and 751).
    let private compress (pvs: Set<int>) =
        let known = knownVersions |> List.map fst
        let idx = known |> List.mapi (fun i v -> v, i) |> Map.ofList

        known
        |> List.filter pvs.Contains
        |> List.fold
            (fun acc v ->
                match acc with
                | (a, b) :: tail when idx[v] = idx[b] + 1 -> (a, v) :: tail
                | _ -> (v, v) :: acc)
            []
        |> List.rev
        |> List.map (fun (a, b) -> if a = b then string a else sprintf "%d–%d" a b)
        |> String.concat ", "

    type private ManifestPacket =
        {
            Key: string
            Ranges: (int * int) list
        }

    let private readManifest (path: string) =
        use doc = JsonDocument.Parse(File.ReadAllText path)

        [
            for p in doc.RootElement.GetProperty("packets").EnumerateObject() ->
                {
                    Key = p.Name
                    Ranges =
                        [
                            for r in p.Value.EnumerateArray() ->
                                r.GetProperty("from").GetInt32(), r.GetProperty("to").GetInt32()
                        ]
                }
        ]

    /// Optional difficulty annotations from `facts stats --format json`: id -> (score, tier).
    let private readStats (path: string) =
        use doc = JsonDocument.Parse(File.ReadAllText path)

        [
            for p in doc.RootElement.GetProperty("Packets").EnumerateArray() ->
                p.GetProperty("Id").GetString(),
                (p.GetProperty("Score").GetInt32(), p.GetProperty("Tier").GetString())
        ]
        |> Map.ofList

    /// Class names of generated packet files currently allowlisted as TODO(codegen) stubs.
    let private readStubs (allowlistPath: string) =
        if not (File.Exists allowlistPath) then
            Set.empty
        else
            File.ReadAllLines allowlistPath
            |> Array.map (fun l -> l.Trim().Replace('\\', '/'))
            |> Array.filter (fun l -> l.StartsWith "Packets/" && l.EndsWith ".cs")
            |> Array.map (fun l -> Path.GetFileNameWithoutExtension l)
            |> Set.ofArray

    /// Build the coverage report. Returns (markdown, one-line summary for stdout).
    let run
        (manifestPath: string)
        (allowlistPath: string)
        (statsPath: string option)
        (packets: PacketSpec list)
        : string * string =
        let manifest = readManifest manifestPath
        let stats = statsPath |> Option.map readStats
        let stubs = readStubs allowlistPath
        let specByKey = packets |> List.map (fun p -> PacketIds.key p, p) |> Map.ofList
        let manifestKeys = manifest |> List.map (fun m -> m.Key) |> Set.ofList
        let pvs = knownVersions |> List.map fst

        let existsAt (m: ManifestPacket) pv =
            m.Ranges |> List.exists (fun (a, b) -> pv >= a && pv <= b)

        let specCovers (p: PacketSpec) pv =
            contains pv p.Since && p.Layouts |> List.exists (fun l -> contains pv l.Range)

        // Per manifest packet: which known pvs exist, and which of those the spec covers.
        let rows =
            [
                for m in manifest ->
                    let spec = specByKey.TryFind m.Key
                    let existing = pvs |> List.filter (existsAt m) |> Set.ofList

                    let covered =
                        match spec with
                        | Some p -> existing |> Set.filter (specCovers p)
                        | None -> Set.empty

                    m, spec, existing, covered
            ]

        let orphanSpecs =
            packets
            |> List.filter (fun p -> not (manifestKeys.Contains(PacketIds.key p)))

        let sb = StringBuilder()
        let line (s: string) = sb.AppendLine s |> ignore

        line "# Карта покрытия мультиверсии"
        line ""
        line "Автогенерат: `dotnet run -- coverage` — не править руками."
        line ""

        line
            "Вселенная пакетов — `Spec/protocol-ids.json` (манифест из McProtoFacts), спеки — то,"

        line
            "что реально лежит в `Spec/Packets/**`. «Покрыто» на версии значит: спек есть, версия"

        line
            "внутри его диапазона поддержки и есть wire-layout на неё. «Стаб» — спек есть, но"

        line
            "генерат в `Spec/todo-allowlist.txt`: компилируется, работать не будет. Снапшоты"

        line "(pv 737–750, 752) не считаются."
        line ""

        // ---- summary per version ----------------------------------------------------
        line "## Итог по версиям"
        line ""
        line "| pv | версия | пакетов | покрыто | из них стабы | осталось |"
        line "|---:|---|---:|---:|---:|---:|"

        for pv, name in knownVersions do
            let total =
                rows |> List.sumBy (fun (_, _, ex, _) -> if ex.Contains pv then 1 else 0)

            let covered =
                rows |> List.sumBy (fun (_, _, _, cov) -> if cov.Contains pv then 1 else 0)

            let stubbed =
                rows
                |> List.sumBy (fun (_, spec, _, cov) ->
                    match spec with
                    | Some p when cov.Contains pv && stubs.Contains p.ClassName -> 1
                    | _ -> 0)

            line (sprintf "| %d | %s | %d | %d | %d | %d |" pv name total covered stubbed (total - covered))

        line ""

        // ---- specs with wire gaps ---------------------------------------------------
        let gapped =
            rows
            |> List.choose (fun (m, spec, existing, covered) ->
                match spec with
                | Some p when not (Set.isEmpty (existing - covered)) -> Some(p, m.Key, existing - covered)
                | _ -> None)

        if not (List.isEmpty gapped) then
            line "## Спеки с дырами покрытия"
            line ""
            line "Пакет существует на версии, а wire-layout на неё в спеке нет — `Read` кинет"
            line "`NotSupportedException`."
            line ""

            for p, key, missing in gapped do
                line (sprintf "- **%s** (`%s`): дыра %s" p.ClassName key (compress missing))

            line ""

        // ---- stubs ------------------------------------------------------------------
        let stubRows =
            rows
            |> List.choose (fun (_, spec, _, _) ->
                spec |> Option.filter (fun p -> stubs.Contains p.ClassName))

        if not (List.isEmpty stubRows) then
            line "## Стабы (todo-allowlist)"
            line ""

            for p in stubRows do
                line (sprintf "- %s" p.ClassName)

            line ""

        // ---- backlog: no spec at all ------------------------------------------------
        line "## Нет спека"
        line ""

        match stats with
        | Some _ -> line "Отсортировано по сложности из facts stats — простое сверху."
        | None ->
            line "Без тиров сложности; чтобы отсортировать по простоте, прогони"
            line "`scripts/facts.ps1 stats --format json > stats.json` и добавь `--stats stats.json`."

        line ""

        let missingByNs =
            rows
            |> List.filter (fun (_, spec, _, _) -> Option.isNone spec)
            |> List.groupBy (fun (m, _, _, _) ->
                match m.Key.Split '.' with
                | [| ns; dir; _ |] -> sprintf "%s.%s" ns dir
                | _ -> m.Key)

        for ns, group in missingByNs do
            let nsTotal =
                rows
                |> List.sumBy (fun (m, _, _, _) -> if m.Key.StartsWith(ns + ".") then 1 else 0)

            line (sprintf "### %s — осталось %d из %d" ns group.Length nsTotal)
            line ""

            let annotated =
                group
                |> List.map (fun (m, _, existing, _) ->
                    let shortName = m.Key.Substring(ns.Length + 1)

                    let score, tier =
                        match stats |> Option.bind (Map.tryFind m.Key) with
                        | Some(s, t) -> s, t
                        | None -> System.Int32.MaxValue, ""

                    let span =
                        if existing = Set.ofList pvs then
                            ""
                        else
                            sprintf " (только %s)" (compress existing)

                    shortName, tier, score, span)
                |> List.sortBy (fun (name, _, score, _) -> score, name)

            for name, tier, score, span in annotated do
                if tier = "" then
                    line (sprintf "- %s%s" name span)
                else
                    line (sprintf "- %s — %s (score %d)%s" name tier score span)

            line ""

        // ---- orphan specs (key not in manifest) -------------------------------------
        if not (List.isEmpty orphanSpecs) then
            line "## Спеки без записи в манифесте"
            line ""
            line "`PacketIds.key` спека не нашёлся в `Spec/protocol-ids.json` — опечатка в"
            line "`protoId` или устаревший манифест (`scripts/generate-ids-manifest.ps1`)."
            line ""

            for p in orphanSpecs do
                line (sprintf "- %s (`%s`)" p.ClassName (PacketIds.key p))

            line ""

        let specCount = packets.Length
        let manifestCount = manifest.Length

        let latestPv = pvs |> List.max

        let latestCovered =
            rows |> List.sumBy (fun (_, _, _, cov) -> if cov.Contains latestPv then 1 else 0)

        let latestTotal =
            rows |> List.sumBy (fun (_, _, ex, _) -> if ex.Contains latestPv then 1 else 0)

        let summary =
            sprintf
                "coverage: спеков %d, пакетов в манифесте %d; на pv %d покрыто %d/%d; дыры wire: %d, стабов: %d"
                specCount
                manifestCount
                latestPv
                latestCovered
                latestTotal
                gapped.Length
                stubRows.Length

        sb.ToString(), summary
