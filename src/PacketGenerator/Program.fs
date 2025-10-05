open System.Diagnostics
open System.Linq
open System.Text.Json
open PacketGenerator.Extensions
open PacketGenerator.History
open PacketGenerator.Types
open PacketGenerator.Core
open PacketGenerator.Utils
open Protodef
open TruePath
open TruePath.SystemIo


let artifacts = ArtifactsPathHelper.ArtifactsPath


let protoMap =
    ProtocolLoader.LoadProtocolsAsync(735, 772)
    |> Async.AwaitTask
    |> Async.RunSynchronously

let allTypes = protoMap.AllTypesPath()


let packets = allTypes |> Array.filter (fun x -> x.Name |> NameUtils.isPacket)

let types =
    allTypes |> Array.filter ((fun x -> x.Name |> NameUtils.isPacket) >> not)



let getDir (p: string) (basePath: AbsolutePath) =
    let parts = p.Split(".")

    match parts with
    | [| state; side; name |] -> basePath / state / side
    | _ -> basePath / "types"

let diffDir = artifacts / "diff"

diffDir.CreateClearDirectory()

let un = packets |> Array.map (fun p -> getDir p.Path diffDir) |> uniquePaths

for gg in un do
    gg.CreateClearDirectory()

let historyToDict (history: TypeStructureHistory) =
    history |> Seq.map (fun x -> (x.Interval.ToString(), x.Structure)) |> dict

let historyToJson (h: TypeStructureHistory) =
    let asDict = historyToDict h
    JsonSerializer.Serialize(asDict, ProtodefType.DefaultJsonOptions)

for p in packets do
    let diff = HistoryBuilder.buildForPath p.Path protoMap
    let dir = getDir p.Path diffDir
    let file = dir / $"{p.Name}.json"

    if file.Exists() then
        failwith $"File {file} exists"

    let d = diff |> historyToDict
    let json = JsonSerializer.Serialize(d, ProtodefType.DefaultJsonOptions)
    file.WriteAllText(json)


// play.toClient.xxx == play.toServer.xxx
// or xxx == xxx
let equalNs p1 p2 =
    let parts1 = p1.Path.Split(".")
    let parts2 = p2.Path.Split(".")
    match parts1, parts2 with
    | [|a; b;_ |], [|d; e;_|] ->
        a = d && b = e
    | [|a|], [|b|] -> a=b
    | _ -> false

let pairs =
    [ for i in 0 .. packets.Length - 1 do
        for j in i+1 .. packets.Length - 1 do
            yield packets.[i], packets.[j] ]
    |> Seq.filter (fun x-> x ||> equalNs)


let ps = [|"play.toServer.packet_chat"; "play.toServer.packet_chat_message"|]

let isPs p1 p2 = ps.Contains(p1.Path) || ps.Contains(p2.Path)

for p1, p2 in pairs do
    let diff1 = HistoryBuilder.buildForPath p1.Path protoMap
    let diff2 = HistoryBuilder.buildForPath p2.Path protoMap

    if HistoryBuilder.canMerge diff1 diff2 then
        
        if isPs p1 p2 then
            let testFile = artifacts / "testMerge.json"
            let merged = HistoryBuilder.merge diff1 diff2
            let json = historyToJson merged
            testFile.WriteAllText(json)
        
        printfn $"cant merge Type1: {p1.Name} ({p1.Path}); Type2: {p2.Name} ({p2.Path})"
