// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading.Tasks
open System.Xml.Linq
open Humanizer
open PacketGenerator.Core
open PacketGenerator.Protocol
open Protodef
open PacketGenerator.Extensions
open TruePath.SystemIo


let artifacts = ArtifactsPathHelper.ArtifactsPath

let test1 = artifacts / "test1.json"
let test2 = artifacts / "test2.json"

let test1Json = test1.ReadAllText()
let test2Json = test2.ReadAllText()

let test1Obj = JsonSerializer.Deserialize<ProtodefType>(test1Json, ProtodefType.DefaultJsonOptions)
let test2Obj = JsonSerializer.Deserialize<ProtodefType>(test2Json, ProtodefType.DefaultJsonOptions)

let eq = test1Obj.Equals(test2Obj)



let protoMap =
    ProtocolLoader.LoadProtocolsAsync(735, 772)
    |> Async.AwaitTask
    |> Async.RunSynchronously


let isPacket (s: string) = s.StartsWith("packet_") || s = "packet"

let filterTypes = [| "native" |]

let allTypes =
    protoMap.Protocols
    |> Seq.map _.Value.Protocol.GetAllTypes()
    |> Seq.concat
    |> Seq.filter (_.IsCustom(filterTypes) >> not)
    |> Seq.map _.ParentName
    |> Seq.filter (isPacket >> not)
    |> Seq.map _.Pascalize()
    |> Set

let testDir = artifacts / "test"
testDir.CreateDirectory()
for t in allTypes do

    let typeDiff = TypeDiffHistory.diffForType protoMap t

    let json = typeDiff.ToJson().ToJsonString(ProtodefType.DefaultJsonOptions)
    
    let jsonFile = testDir / $"{t}.json"

    jsonFile.WriteAllText(json)






