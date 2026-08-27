/// Cross-checks the packet manifest against the official Minecraft Wiki.
///
/// `Spec/protocol-ids.json` is derived from McProtoFacts, which is derived from
/// minecraft-data. Everything downstream — the specs, the generated C#, the coverage
/// map — inherits whatever that chain believes. So a packet missing from minecraft-data
/// is invisible to `dotnet run -- coverage`: the packet universe and the thing measured
/// against it come from the same source.
///
/// These tests add an independent axis. The expected numbers in `wiki-packet-counts.json`
/// come from the wiki, which nothing in this repo derives from. A failure means the two
/// sources disagree about how many packets a protocol version has — which is either a gap
/// in minecraft-data or a gap in the wiki, and worth a look either way.
module McProtocol.Tests.PacketCountTests

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open Xunit

let private states = [ "handshaking"; "status"; "login"; "configuration"; "play" ]
let private directions = [ "clientbound"; "serverbound" ]

/// protodef writes the direction as `toClient`/`toServer`; the wiki calls it
/// clientbound/serverbound.
let private directionOf =
    function
    | "toClient" -> Some "clientbound"
    | "toServer" -> Some "serverbound"
    | _ -> None

let private readJson name =
    let path = Path.Combine(AppContext.BaseDirectory, name)

    if not (File.Exists path) then
        failwithf "%s is missing next to the test binary — check the Content items in the fsproj" name

    // ReadAllText strips the UTF-8 BOM that the manifest carries; JsonDocument would choke on it.
    JsonDocument.Parse(File.ReadAllText path)

/// How many packets the manifest declares for a given protocol version, state and direction.
/// A packet counts on every protocol number inside one of its `from`–`to` ranges.
let private manifestCounts =
    lazy
        (let doc = readJson "protocol-ids.json"
         let counts = Dictionary<int * string * string, int>()

         for packet in doc.RootElement.GetProperty("packets").EnumerateObject() do
             match packet.Name.Split('.') with
             | [| state; direction; _ |] ->
                 match directionOf direction with
                 | Some direction ->
                     for range in packet.Value.EnumerateArray() do
                         let lo = range.GetProperty("from").GetInt32()
                         let hi = range.GetProperty("to").GetInt32()

                         for pv in lo..hi do
                             let key = (pv, state, direction)

                             counts[key] <-
                                 match counts.TryGetValue key with
                                 | true, n -> n + 1
                                 | _ -> 1
                 | None -> ()
             | _ -> ()

         counts)

type private WikiVersion =
    {
        Protocol: int
        McVersion: string
        OldId: int option
        /// `None` means the state does not exist in this protocol version — the configuration
        /// state only arrives in 764. That is different from "exists and is empty".
        Counts: Map<string * string, int option>
    }

let private wiki =
    lazy
        (let root = (readJson "wiki-packet-counts.json").RootElement

         let undocumented =
             root.GetProperty("undocumented").GetProperty("protocols").EnumerateArray()
             |> Seq.map (fun e -> e.GetInt32())
             |> Set.ofSeq

         let versions =
             [
                 for v in root.GetProperty("versions").EnumerateArray() ->
                     let cells = v.GetProperty("counts")

                     {
                         Protocol = v.GetProperty("protocol").GetInt32()
                         McVersion = v.GetProperty("mcVersion").GetString()
                         OldId =
                             match v.GetProperty("oldid") with
                             | o when o.ValueKind = JsonValueKind.Null -> None
                             | o -> Some(o.GetInt32())
                         Counts =
                             Map.ofList
                                 [
                                     for state in states do
                                         for direction in directions do
                                             let cell = cells.GetProperty(state).GetProperty(direction)

                                             yield
                                                 (state, direction),
                                                 (if cell.ValueKind = JsonValueKind.Null then
                                                      None
                                                  else
                                                      Some(cell.GetInt32()))
                                 ]
                     }
             ]

         versions, undocumented)

/// One test case per documented protocol version, so a failure names the version.
type WikiVersions() =
    static member All: obj array seq = fst wiki.Value |> Seq.map (fun v -> [| box v.Protocol |])

let private revisionLink version =
    match version.OldId with
    | Some id -> sprintf "https://minecraft.wiki/?oldid=%d" id
    | None -> "https://minecraft.wiki/w/Java_Edition_protocol/Packets (live page)"

[<Theory; MemberData("All", MemberType = typeof<WikiVersions>)>]
let ``manifest packet counts agree with the wiki`` (protocol: int) =
    let version = fst wiki.Value |> List.find (fun v -> v.Protocol = protocol)
    let counts = manifestCounts.Value

    let inManifest state direction =
        match counts.TryGetValue((protocol, state, direction)) with
        | true, n -> n
        | _ -> 0

    let mismatches =
        [
            for state in states do
                for direction in directions do
                    let got = inManifest state direction

                    match version.Counts |> Map.find (state, direction) with
                    | Some expected when expected <> got ->
                        yield sprintf "  %-13s %-11s wiki %3d, manifest %3d" state direction expected got
                    | None when got <> 0 ->
                        yield
                            sprintf "  %-13s %-11s the wiki has no such state here, manifest has %d" state direction got
                    | _ -> ()
        ]

    Assert.True(
        List.isEmpty mismatches,
        sprintf
            "protocol %d (%s) disagrees with the wiki:\n%s\n\nWiki revision: %s"
            protocol
            version.McVersion
            (String.concat "\n" mismatches)
            (revisionLink version)
    )

/// Guards the reference data itself: adding a protocol version to `Coverage.knownVersions`
/// must not silently skip the wiki cross-check.
[<Fact>]
let ``every supported protocol version is documented or explicitly excluded`` () =
    let versions, undocumented = wiki.Value

    let accountedFor =
        Set.union (versions |> List.map (fun v -> v.Protocol) |> Set.ofList) undocumented

    let missing =
        McProtocol.Codegen.Coverage.knownVersions
        |> List.map fst
        |> List.filter (accountedFor.Contains >> not)

    Assert.True(
        List.isEmpty missing,
        sprintf
            "wiki-packet-counts.json says nothing about protocol %s. Add its counts, or list it under \"undocumented\" with a reason."
            (missing |> List.map string |> String.concat ", ")
    )
