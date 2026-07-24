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
    let key (p: PacketSpec) =
        let name = defaultArg p.ProtoName (snake p.ClassName)
        sprintf "%s.%s.%s" (stateKey p.State) (dirKey p.Direction) name

    /// Enrich every packet with `Ids` from the manifest at `manifestPath`. Returns the enriched
    /// specs alongside human-readable warnings for packets with no matching manifest entry.
    let enrich (manifestPath: string) (packets: PacketSpec list) : PacketSpec list * string list =
        use doc = JsonDocument.Parse(File.ReadAllText manifestPath)
        let table = doc.RootElement.GetProperty "packets"
        let mutable warnings = []

        let enriched =
            packets
            |> List.map (fun p ->
                let k = key p

                match table.TryGetProperty k with
                | true, ranges ->
                    let ids =
                        [
                            for r in ranges.EnumerateArray() ->
                                r.GetProperty("from").GetInt32(),
                                r.GetProperty("to").GetInt32(),
                                System.Convert.ToInt32(r.GetProperty("id").GetString(), 16)
                        ]

                    { p with Ids = ids }
                | _ ->
                    warnings <- warnings @ [ sprintf "no packet id in manifest for %s (%s)" p.ClassName k ]
                    p)

        enriched, warnings
