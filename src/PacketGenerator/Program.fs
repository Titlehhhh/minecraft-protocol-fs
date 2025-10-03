
open System.Text.Json
open PacketGenerator.Types
open PacketGenerator.Core
open Protodef
open Test
open TruePath
open TruePath.SystemIo


let artifacts = ArtifactsPathHelper.ArtifactsPath


let des (p: AbsolutePath) =
    let json = p.ReadAllText()
    JsonSerializer.Deserialize<ProtodefType>(json, ProtodefType.DefaultJsonOptions)

let obj1 = des (artifacts / "test1.json")
let obj2 = des (artifacts / "test2.json")

let eq = obj1.Equals(obj2)

printf $"{eq}"





let protoMap =
    ProtocolLoader.LoadProtocolsAsync(735, 772)
    |> Async.AwaitTask
    |> Async.RunSynchronously

let allTypes = protoMap.AllTypesPath()

// filter types "packet"
let isNamePacket (s: NamePathPair) = s.Name.StartsWith("Packet")
let packets = allTypes |> Array.filter isNamePacket
let types = allTypes |> Array.filter (not << isNamePacket)



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
    let diff = TypeDiff.find p.Path protoMap
    let dir = getDir p.Path diffDir
    let file = dir / $"{p.Name}.json"
    if file.Exists() then failwith $"File {file} exists" 
    
    let d = diff |> historyToDict
    let json = JsonSerializer.Serialize(d, ProtodefType.DefaultJsonOptions)
    file.WriteAllText(json) 
       
    
exit 0
let pairs = packets |> Seq.pairwise

for p1, p2 in pairs do
    let diff1 = TypeDiff.find p1.Path protoMap
    let diff2 = TypeDiff.find p2.Path protoMap
    
    if TypeDiff.canMerge diff1 diff2 then
        printfn $"cant merge Type1: {p1.Name} ({p2.Path}); Type2: {p2.Name} ({p2.Path})"