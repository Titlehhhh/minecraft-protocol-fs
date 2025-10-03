
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


let packets = allTypes |> Array.filter (fun x-> x.Name |> NameUtils.isPacket) 
let types = allTypes |> Array.filter ((fun x-> x.Name |> NameUtils.isPacket) >> not)



let getDir (p: string) (basePath: AbsolutePath) =
    let parts = p.Split(".")
    match parts with
    | [| state; side; name |] -> basePath / state / side
    | _ -> basePath / "types"
    
let diffDir = artifacts / "diff"

diffDir.CreateClearDirectory()

let un = packets
        |> Array.map (fun p -> getDir p.Path diffDir)
        |> uniquePaths

for gg in un do
    gg.CreateClearDirectory()

let historyToDict (history: TypeStructureHistory) =
    history
    |> Seq.map (fun x-> (x.Interval.ToString(), x.Structure))
    |> dict

for p in packets do    
    let diff = HistoryBuilder.buildForPath p.Path protoMap
    let dir = getDir p.Path diffDir
    let file = dir / $"{p.Name}.json"
    if file.Exists() then failwith $"File {file} exists" 
    
    let d = diff |> historyToDict
    let json = JsonSerializer.Serialize(d, ProtodefType.DefaultJsonOptions)
    file.WriteAllText(json) 
       
    
exit 0
let pairs = packets |> Seq.pairwise

for p1, p2 in pairs do
    let diff1 = HistoryBuilder.buildForPath p1.Path protoMap
    let diff2 = HistoryBuilder.buildForPath p2.Path protoMap
    
    if HistoryBuilder.canMerge diff1 diff2 then
        printfn $"cant merge Type1: {p1.Name} ({p2.Path}); Type2: {p2.Name} ({p2.Path})"