namespace McProtocol.Codegen

open System.IO
open System.Text.Json
open McProtocol.Dsl

/// Enriches `PacketSpec`s with numeric packet ids from the McProtoFacts-derived manifest
/// (`Spec/protocol-ids.json`, see `scripts/generate-ids-manifest.ps1`). Self-contained: manifest
/// IO, the manifest-key derivation from a spec, and the enrichment pass all live here.
module PacketIds =

    let private stateKey s =
        match s with
        | Handshaking -> "handshaking"
        | Status -> "status"
        | Login -> "login"
        | Configuration -> "configuration"
        | Play -> "play"

    let private dirKey d =
        match d with
        | Clientbound -> "toClient"
        | Serverbound -> "toServer"

    /// `LegacyServerListPingPacket` -> `legacy_server_list_ping` (strip trailing `Packet`, then
    /// snake_case the rest).
    let private snake (name: string) =
        let core = if name.EndsWith "Packet" then name.[.. name.Length - 7] else name

        core
        |> Seq.mapi (fun i c ->
            if i > 0 && System.Char.IsUpper c then
                sprintf "_%c" (System.Char.ToLower c)
            else
                string (System.Char.ToLower c))
        |> String.concat ""

    /// The manifest key for a packet: `<state>.<direction>.<protodefName>`, where the protodef
    /// name is the spec's `protoId` override if given, else the snake_cased class name.
    /// This is the spec's identity and never changes.
    let key (p: PacketSpec) =
        let name = defaultArg p.ProtoName (snake p.ClassName)
        sprintf "%s.%s.%s" (stateKey p.State) (dirKey p.Direction) name

    /// Manifest keys of the spec's alias names: the same wire packet carried under another
    /// protodef name (an upstream rename — `spectate_entity` at 775/777, `spectator_action` at
    /// 776). Aliases never change the identity (`key` stays the primary name); they only widen
    /// the id coverage taken from the manifest.
    let aliasKeys (p: PacketSpec) =
        p.ProtoAliases
        |> List.distinct
        |> List.map (fun alias -> sprintf "%s.%s.%s" (stateKey p.State) (dirKey p.Direction) alias)

    /// Every manifest key a spec answers to: the primary key plus its aliases.
    let keys (p: PacketSpec) = key p :: aliasKeys p

    /// The `(from, to, id)` triples of one manifest entry, in manifest order.
    let private readRanges (ranges: JsonElement) =
        [
            for r in ranges.EnumerateArray() ->
                r.GetProperty("from").GetInt32(),
                r.GetProperty("to").GetInt32(),
                System.Convert.ToInt32(r.GetProperty("id").GetString(), 16)
        ]

    /// Merge the id ranges of several manifest entries (one packet renamed upstream): identical
    /// triples collapse, the rest is ordered by the first covered version.
    let private mergeIds (ranges: (int * int * int) list) =
        ranges |> List.sortBy (fun (lo, hi, id) -> lo, hi, id) |> List.distinct

    /// Enrich every packet with `Ids` from the manifest at `manifestPath`. The primary key and
    /// every alias key of a spec are looked up and their ranges merged, so a packet that
    /// upstream renamed between protocol versions keeps one type and one full range of ids.
    /// Returns the enriched specs alongside human-readable warnings for packets with no
    /// matching manifest entry.
    let enrich (manifestPath: string) (packets: PacketSpec list) : PacketSpec list * string list =
        use doc = JsonDocument.Parse(File.ReadAllText manifestPath)
        let table = doc.RootElement.GetProperty "packets"
        let mutable warnings = []

        let enriched =
            packets
            |> List.map (fun p ->
                let k = key p

                let primaryRanges =
                    match table.TryGetProperty k with
                    | true, ranges -> readRanges ranges
                    | _ -> []

                let aliasRanges =
                    aliasKeys p
                    |> List.map (fun ak ->
                        match table.TryGetProperty ak with
                        | true, ranges -> readRanges ranges
                        | _ ->
                            warnings <-
                                warnings
                                @ [ sprintf "no packet id in manifest for alias of %s (%s)" p.ClassName ak ]

                            [])
                    |> List.concat

                if primaryRanges.IsEmpty then
                    if aliasRanges.IsEmpty then
                        warnings <- warnings @ [ sprintf "no packet id in manifest for %s (%s)" p.ClassName k ]
                    else
                        warnings <-
                            warnings
                            @ [
                                sprintf "no packet id in manifest for %s (%s) — ids taken from its alias" p.ClassName k
                            ]

                { p with Ids = mergeIds (primaryRanges @ aliasRanges) })

        enriched, warnings
