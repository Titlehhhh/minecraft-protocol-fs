open System
open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.Reflection
open System.Text.Json
open Humanizer
open PacketGenerator.CodeGeneration
open PacketGenerator.Extensions
open PacketGenerator.History
open PacketGenerator.Types
open PacketGenerator.Core

open PacketGenerator.Utils
open Protodef
open Protodef.Enumerable
open TruePath
open TruePath.SystemIo


let artifacts = ArtifactsPathHelper.ArtifactsPath


let protoMap =
    ProtocolLoader.LoadProtocolsAsync(735, 772)
    |> Async.AwaitTask
    |> Async.RunSynchronously

let allTypes = protoMap.AllTypesPath()


let packets1 = allTypes |> Array.filter (fun x -> x.Name |> Filters.isPacket)

let types =
    allTypes |> Array.filter ((fun x -> x.Name |> Filters.isPacket) >> not)

let packets = packets1

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
        printfn $"File {file} exists. Path: {p.Path}"
    else
        let d = diff |> historyToDict
        let json = JsonSerializer.Serialize(d, ProtodefType.DefaultJsonOptions)
        file.WriteAllText($"//{p.Path}\n{json}")


// play.toClient.xxx == play.toServer.xxx
// or xxx == xxx
let equalNs p1 p2 =
    let parts1 = p1.Path.Split(".")
    let parts2 = p2.Path.Split(".")

    match parts1, parts2 with
    | [| a; b; _ |], [| d; e; _ |] -> a = d && b = e
    | [| a |], [| b |] -> a = b
    | _ -> false

let pairs =
    [ for i in 0 .. packets.Length - 1 do
          for j in i + 1 .. packets.Length - 1 do
              yield packets.[i], packets.[j] ]
    |> Seq.filter (fun x -> x ||> equalNs)

let codeGenDir = artifacts / "codeGen"

codeGenDir.CreateClearDirectory()

let mutable generated = 0
let allPackets = packets.Length
//Code gen
let messages1 = ResizeArray<string>()
let messages2 = ResizeArray<string>()


for p in packets do
    let diff = HistoryBuilder.buildForPath p.Path protoMap
    
    let isPrim = diff |> Seq.forall (fun x -> 
        match x.Structure with
        | None -> true
        | Some t when t.IsContainer() ->
            let cont: ProtodefContainer = t :?> ProtodefContainer
            cont.isSimpleTypeForGenerator
        | _ -> false)
    
    try    
        if isPrim then
            let spec = Helpers.toSpec diff p.Name  
            let code = ClassGenerator.generate spec
            (codeGenDir / $"{p.Path.Pascalize()}.cs").WriteAllText(code)
            generated <- generated + 1
        else
            $"Non-primitive: {p.Path}" |> messages1.Add
    with
    | :? Exception as ex ->
        $"Exception {ex.Message} in path: {p.Path}" |> messages2.Add

messages1 |> Seq.iter (fun x-> printfn $"{x}")
printfn ""
messages2 |> Seq.sort |> Seq.iter (fun x-> printfn $"{x}")

printfn ""
printfn $"Generated {generated}/{allPackets}"

exit 0


let ps = [| "play.toServer.packet_chat"; "play.toServer.packet_chat_message" |]

let isPs p1 p2 =
    ps.Contains(p1.Path) || ps.Contains(p2.Path)

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
